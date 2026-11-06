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

        [BindProperty(SupportsGet = true)]
        public string Code { get; set; } = string.Empty;

        // Only used by the cancel form
        [BindProperty]
        public string Phone { get; set; } = string.Empty;

        // Only used by the food order form
        [BindProperty]
        public string OrderPhone { get; set; } = string.Empty;

        // Only used by the pay form
        [BindProperty]
        public string PayPhone { get; set; } = string.Empty;

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

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(Code))
                return RedirectToPage("/Index");

            if (!await LoadAsync(ct))
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostCancelAsync(CancellationToken ct)
        {
            try
            {
                await _reservations.CancelAsync(Code, Phone, ct);
                ToastSuccess("Your booking has been cancelled.");
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage(new { code = Code });
        }

        // Reads one quantity per menu item from the form (named qty_{id}) and places them as one order
        public async Task<IActionResult> OnPostOrderAsync(CancellationToken ct)
        {
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
                var order = await _orders.PlaceOrderAsync(Code, OrderPhone, selections, ct);
                ToastSuccess($"Order placed: {order.Items.Count} item(s), total {Money.Inr(order.Total())}.");
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage(new { code = Code });
        }

        // Step 1: the guest proves the booking with the phone number, then picks which order to pay
        public async Task<IActionResult> OnPostPayAsync(CancellationToken ct)
        {
            try
            {
                var orders = await _payments.GetPayableOrdersAsync(Code, PayPhone, ct);
                PayableOrders = orders.Where(o => !o.IsPaid).ToList();

                if (PayableOrders.Count == 0)
                    ToastSuccess("All food orders for this booking are already paid.");

                await LoadAsync(ct);
                return Page();
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
                return RedirectToPage(new { code = Code });
            }
        }

        // Step 2: the guest chose one order. Its amount is read from the order on the checkout page.
        public async Task<IActionResult> OnPostPayOrderAsync(long orderId, CancellationToken ct)
        {
            try
            {
                var orders = await _payments.GetPayableOrdersAsync(Code, PayPhone, ct);
                if (!orders.Any(o => o.Id == orderId && !o.IsPaid))
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

        private async Task<bool> LoadAsync(CancellationToken ct)
        {
            Reservation = await _reservations.FindByCodeAsync(Code, ct);
            if (Reservation is null)
                return false;

            Restaurant = await _reservations.GetRestaurantAsync(Reservation.RestaurantId, ct);

            CanOrderFood = Reservation.AcceptsFoodOrders(_reservations.LocalNow());
            if (CanOrderFood)
                Menu = await _menu.GetAvailableItemsAsync(Reservation.RestaurantId, ct);

            return true;
        }
    }
}
