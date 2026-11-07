using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;
using DineBook.Web.Infrastructure;

namespace DineBook.Web.Pages.Reservations
{
    [EnableRateLimiting("checkout")]
    public class DetailsModel : AppPageModel
    {
        private readonly IReservationService _reservations;
        private readonly IMenuService _menu;
        private readonly IFoodOrderService _orders;
        private readonly IPaymentService _payments;
        private readonly BookingAccess _access;

        public DetailsModel(
            IReservationService reservations,
            IMenuService menu,
            IFoodOrderService orders,
            IPaymentService payments,
            BookingAccess access)
        {
            _reservations = reservations;
            _menu = menu;
            _orders = orders;
            _payments = payments;
            _access = access;
        }

        [BindProperty(SupportsGet = true)]
        public string Code { get; set; } = string.Empty;

        // Only used once, to unlock a booking made by someone else on this device
        [BindProperty]
        public string UnlockPhone { get; set; } = string.Empty;

        public Reservation Reservation { get; private set; }

        public Restaurant Restaurant { get; private set; }

        // True when this visitor may see and change the booking. Otherwise only the unlock form shows.
        public bool IsOwner { get; private set; }

        public IReadOnlyList<Orders> FoodOrders { get; private set; } = Array.Empty<Orders>();

        public IReadOnlyList<IGrouping<string, MenuItem>> MenuGroups { get; private set; } = Array.Empty<IGrouping<string, MenuItem>>();

        public bool CanOrderFood { get; private set; }

        public bool CanCancel => Reservation?.Status == ReservationStatus.Confirmed;

        // Guests can pay until the booking is cancelled or marked as no-show
        public bool CanPay => Reservation is not null
            && Reservation.Status != ReservationStatus.Cancelled
            && Reservation.Status != ReservationStatus.NoShow;

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(Code))
                return RedirectToPage("/Bookings/Index");

            if (!await LoadAsync(ct))
                return NotFound();

            return Page();
        }

        // Asks for the booking phone once. After that this browser keeps access to the booking.
        public async Task<IActionResult> OnPostUnlockAsync(CancellationToken ct)
        {
            var booking = await _reservations.FindByCodeAsync(Code, ct);

            // Same message for an unknown code and a wrong phone, so the code alone reveals nothing
            if (booking is null || !booking.MatchesPhone(UnlockPhone))
            {
                ToastError("We could not find a booking with those details.");
                return RedirectToPage(new { code = Code });
            }

            _access.Remember(booking, saveContact: false);
            ToastSuccess("Booking opened. You will not need the phone number again on this device.");
            return RedirectToPage(new { code = Code });
        }

        public async Task<IActionResult> OnPostOrderAsync(CancellationToken ct)
        {
            var booking = await OwnedBookingAsync(ct);
            if (booking is null)
                return RedirectToLocked();

            var menu = await _menu.GetAvailableItemsAsync(booking.RestaurantId, ct);

            var selections = new List<OrderItemRequest>();
            foreach (var item in menu)
            {
                var raw = Request.Form["qty_" + item.Id].ToString();
                if (int.TryParse(raw, out var quantity) && quantity > 0)
                    selections.Add(new OrderItemRequest(item.Id, quantity));
            }

            try
            {
                // The booking's own phone number is used as the proof, because ownership was checked above
                var order = await _orders.PlaceOrderAsync(Code, booking.CustomerPhone, selections, ct);
                ToastSuccess($"Order placed: {order.Items.Count} item(s), total {DineBook.Web.Helpers.Money.Inr(order.Total())}.");
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage(new { code = Code });
        }

        public async Task<IActionResult> OnPostCancelAsync(CancellationToken ct)
        {
            var booking = await OwnedBookingAsync(ct);
            if (booking is null)
                return RedirectToLocked();

            try
            {
                await _reservations.CancelAsync(Code, booking.CustomerPhone, ct);
                ToastSuccess("Your booking has been cancelled.");
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage(new { code = Code });
        }

        // Pay one food order. Checkout reads the amount from the order, never from this form.
        public async Task<IActionResult> OnPostPayOrderAsync(long orderId, CancellationToken ct)
        {
            var booking = await OwnedBookingAsync(ct);
            if (booking is null)
                return RedirectToLocked();

            try
            {
                var payable = await _payments.GetPayableOrdersAsync(Code, booking.CustomerPhone, ct);
                if (!payable.Any(o => o.Id == orderId && !o.IsPaid))
                {
                    ToastError("That order cannot be paid.");
                    return RedirectToPage(new { code = Code });
                }

                return RedirectToPage("/Checkout", new { id = orderId });
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
                return RedirectToPage(new { code = Code });
            }
        }

        private async Task<Reservation> OwnedBookingAsync(CancellationToken ct)
        {
            var booking = await _reservations.FindByCodeAsync(Code, ct);
            return booking is not null && await _access.OwnsAsync(booking, ct) ? booking : null;
        }

        private RedirectToPageResult RedirectToLocked()
        {
            ToastError("Enter the booking phone number to open this booking first.");
            return RedirectToPage(new { code = Code });
        }

        private async Task<bool> LoadAsync(CancellationToken ct)
        {
            Reservation = await _reservations.FindByCodeAsync(Code, ct);
            if (Reservation is null)
                return false;

            Restaurant = await _reservations.GetRestaurantAsync(Reservation.RestaurantId, ct);
            IsOwner = await _access.OwnsAsync(Reservation, ct);

            if (!IsOwner)
                return true;

            FoodOrders = await _orders.GetOrdersForBookingsAsync(new[] { Reservation.Id }, ct);

            CanOrderFood = Reservation.AcceptsFoodOrders(_reservations.LocalNow());
            if (CanOrderFood)
            {
                var menu = await _menu.GetAvailableItemsAsync(Reservation.RestaurantId, ct);
                MenuGroups = menu.GroupBy(m => string.IsNullOrEmpty(m.Category) ? "Menu" : m.Category).ToList();
            }

            return true;
        }
    }
}
