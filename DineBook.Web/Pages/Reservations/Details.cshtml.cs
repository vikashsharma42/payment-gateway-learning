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
using DineBook.Web.Dtos;
using DineBook.Web.Helpers;
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

        public DetailsModel(
            IReservationService reservations,
            IMenuService menu,
            IFoodOrderService orders,
            IPaymentService payments)
        {
            _reservations = reservations;
            _menu = menu;
            _orders = orders;
            _payments = payments;
        }

        // The restaurant's web address. A booking opens only under its own restaurant.
        [BindProperty(SupportsGet = true)]
        public string Slug { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string Code { get; set; } = string.Empty;

        // The location in the address. The booking's own location is the one that counts (see OnGetAsync).
        [BindProperty(SupportsGet = true)]
        public string LocationSlug { get; set; } = string.Empty;

        // The phone number from the cancel, food order or pay form (one form is posted at a time)
        [BindProperty]
        public BookingPhoneDto Input { get; set; } = new();

        // The edit form: phone number, party size and special requests
        [BindProperty]
        public BookingChangeDto Change { get; set; } = new();

        // Customers can change or cancel until the cutoff before the start time
        public bool CanChange => Reservation is not null && Reservation.CanCustomerChange(_reservations.LocalNow());

        public Reservation Reservation { get; private set; }

        // Unpaid orders of this booking, loaded after the guest enters the booking phone number
        public IReadOnlyList<Orders> PayableOrders { get; private set; } = Array.Empty<Orders>();

        // Guests can pay once the booking is not cancelled or missed
        public bool CanPay => Reservation is not null
            && Reservation.Status != ReservationStatus.Cancelled
            && Reservation.Status != ReservationStatus.NoShow;

        public Restaurant Restaurant { get; private set; }

        public IReadOnlyList<MenuItem> Menu { get; private set; } = Array.Empty<MenuItem>();

        public bool CanOrderFood { get; private set; }

        // The booking advance, shown as a deduction from the food bill
        public decimal BookingAdvance => _payments.BookingAdvance;

        // True when the deposit is still to be paid for this booking
        public bool NeedsDeposit { get; private set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            if (!await LoadAsync(ct))
                return NotFound();

            // The booking's own location decides its address: a wrong location in the URL is corrected
            var bookingLocation = Restaurant.LocationSlugFor(Reservation);
            if (!string.IsNullOrEmpty(bookingLocation)
                && !string.Equals(bookingLocation, LocationSlug, StringComparison.OrdinalIgnoreCase))
                return RedirectToPage(new { slug = Slug, locationSlug = bookingLocation, code = Code });

            // A cancelled booking's refund is retried here until it has gone through
            if (Reservation.Status == ReservationStatus.Cancelled)
                await _payments.RefundCancelledBookingAsync(Code, ct);

            // The edit form starts with the current values
            Change.Guests = Reservation.Guests;
            Change.SpecialRequests = Reservation.SpecialRequests;

            return Page();
        }

        // Changes the party size or special requests. Shows the form again with the messages when the input is invalid.
        public async Task<IActionResult> OnPostUpdateAsync(CancellationToken ct)
        {
            if (!await BelongsToSlugAsync(ct))
                return NotFound();

            if (!ModelState.IsValid)
            {
                if (!await LoadAsync(ct))
                    return NotFound();

                return Page();
            }

            try
            {
                await _reservations.UpdateBookingAsync(Code, DineBook.Domain.ValueObjects.PhoneNumbers.Resolve(Change.CountryCode, Change.Phone), Change.Guests, Change.SpecialRequests ?? string.Empty, ct);
                ToastSuccess("Your booking has been updated.");
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage(new { slug = Slug, locationSlug = LocationSlug, code = Code });
        }

        public async Task<IActionResult> OnPostCancelAsync(CancellationToken ct)
        {
            if (!await BelongsToSlugAsync(ct))
                return NotFound();

            try
            {
                await _reservations.CancelAsync(Code, DineBook.Domain.ValueObjects.PhoneNumbers.Resolve(Input.CountryCode, Input.Phone), ct);

                // The deposit, less the cancellation fee, goes back to the customer
                var refund = await _payments.RefundCancelledBookingAsync(Code, ct);
                ToastSuccess(refund > 0
                    ? $"Your booking has been cancelled. {Money.Inr(refund)} will be refunded."
                    : "Your booking has been cancelled.");
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage(new { slug = Slug, locationSlug = LocationSlug, code = Code });
        }

        // Reads one quantity per menu item from the form (named qty_{id}) and places them as one order
        public async Task<IActionResult> OnPostOrderAsync(CancellationToken ct)
        {
            if (!await BelongsToSlugAsync(ct))
                return NotFound();

            var reservation = await _reservations.FindByCodeAsync(Code, ct);
            if (reservation is null)
                return NotFound();

            var menu = await _menu.GetAvailableItemsAsync(reservation.RestaurantId, ct);

            var selections = new List<OrderItemRequest>();
            foreach (var item in menu)
            {
                var raw = Request.Form["qty_" + item.Id].ToString();
                if (int.TryParse(raw, out var quantity) && quantity > 0)
                    selections.Add(new OrderItemRequest(item.Id, quantity));
            }

            try
            {
                var order = await _orders.PlaceOrderAsync(Code, DineBook.Domain.ValueObjects.PhoneNumbers.Resolve(Input.CountryCode, Input.Phone), selections, ct);
                ToastSuccess($"Order {order.OrderNo} placed with {order.Items.Count} item(s).");
                TempData["Celebrate"] = "1";
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage(new { slug = Slug, locationSlug = LocationSlug, code = Code });
        }

        // Step 1: the guest proves the booking with the phone number, then picks which order to pay
        public async Task<IActionResult> OnPostPayAsync(CancellationToken ct)
        {
            if (!await BelongsToSlugAsync(ct))
                return NotFound();

            try
            {
                var orders = await _payments.GetPayableOrdersAsync(Code, DineBook.Domain.ValueObjects.PhoneNumbers.Resolve(Input.CountryCode, Input.Phone), ct);
                PayableOrders = orders.Where(o => !o.IsPaid).ToList();

                if (PayableOrders.Count == 0)
                    ToastSuccess("All food orders for this booking are already paid.");

                await LoadAsync(ct);
                return Page();
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
                return RedirectToPage(new { slug = Slug, locationSlug = LocationSlug, code = Code });
            }
        }

        // Step 2: the guest chose one order. Its amount is read from the order on the checkout page.
        public async Task<IActionResult> OnPostPayOrderAsync(long orderId, CancellationToken ct)
        {
            if (!await BelongsToSlugAsync(ct))
                return NotFound();

            try
            {
                var orders = await _payments.GetPayableOrdersAsync(Code, DineBook.Domain.ValueObjects.PhoneNumbers.Resolve(Input.CountryCode, Input.Phone), ct);
                if (!orders.Any(o => o.Id == orderId && !o.IsPaid))
                {
                    ToastError("This order can't be paid right now.");
                    return RedirectToPage(new { slug = Slug, locationSlug = LocationSlug, code = Code });
                }

                return RedirectToPage("/Checkout", new { slug = Slug, locationSlug = LocationSlug, id = orderId });
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
                return RedirectToPage(new { slug = Slug, locationSlug = LocationSlug, code = Code });
            }
        }

        // True when the booking exists and belongs to the restaurant named in the web address
        private async Task<bool> BelongsToSlugAsync(CancellationToken ct)
        {
            var restaurant = await _reservations.GetRestaurantBySlugAsync(Slug, ct);
            if (restaurant is null)
                return false;

            var reservation = await _reservations.FindByCodeAsync(Code, ct);
            return reservation is not null && reservation.RestaurantId == restaurant.Id;
        }

        private async Task<bool> LoadAsync(CancellationToken ct)
        {
            Reservation = await _reservations.FindByCodeAsync(Code, ct);
            if (Reservation is null)
                return false;

            Restaurant = await _reservations.GetRestaurantAsync(Reservation.RestaurantId, ct);
            if (Restaurant is null || !string.Equals(Restaurant.Slug, Slug, StringComparison.OrdinalIgnoreCase))
                return false;

            NeedsDeposit = CanPay && BookingAdvance > 0m
                && await _payments.DepositHeldAsync(Reservation.Id, ct) == 0m;

            CanOrderFood = Reservation.AcceptsFoodOrders(_reservations.LocalNow());
            if (CanOrderFood)
                Menu = await _menu.GetAvailableItemsAsync(Reservation.RestaurantId, ct);

            return true;
        }
    }
}
