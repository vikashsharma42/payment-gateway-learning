using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Web.Dtos;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Payments
{
    public class ResultModel : AppPageModel
    {
        private readonly IPaymentService _payments;

        public ResultModel(IPaymentService payments)
        {
            _payments = payments;
        }

        // Cashfree sends the order id as "order_id" in the query string (see PaymentResultQuery)
        public PaymentResultQuery Query { get; private set; } = new();

        // The folder name Payment is also a namespace, so the entity needs an alias here
        public Payment CurrentOrder { get; private set; }

        // The booking this payment belongs to, for the "back to your booking" link
        public string ReservationCode { get; private set; } = string.Empty;

        public string RestaurantSlug { get; private set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync([FromQuery] PaymentResultQuery query, CancellationToken ct)
        {
            Query = query;
            if (string.IsNullOrWhiteSpace(Query.OrderId))
                return NotFound();

            // Confirms with Cashfree and updates the stored status
            CurrentOrder = await _payments.GetOrderStatusAsync(Query.OrderId, ct);
            if (CurrentOrder is null)
                return NotFound();

            var links = await _payments.GetBookingLinksAsync(CurrentOrder.OrderId, ct);
            ReservationCode = links?.ReservationCode ?? string.Empty;
            RestaurantSlug = links?.RestaurantSlug ?? string.Empty;

            switch (CurrentOrder.Status)
            {
                case PaymentStatus.Paid:
                    ToastSuccess("Payment successful. Thank you for your order.");

                    // Shown on this page, so the bubbles play now rather than on a later visit
                    TempData["Celebrate"] = "1";
                    break;
                case PaymentStatus.Failed:
                    ToastError("Payment was not completed. You can try again.");
                    break;
            }

            return Page();
        }
    }
}
