using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Infrastructure.Database;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Payments
{
    public class ResultModel : AppPageModel
    {
        private readonly IPaymentService _payments;
        private readonly ApplicationDbContext _db;

        public ResultModel(IPaymentService payments, ApplicationDbContext db)
        {
            _payments = payments;
            _db = db;
        }

        // Cashfree sends the order id as "order_id" in the query string
        [BindProperty(SupportsGet = true, Name = "order_id")]
        public string OrderId { get; set; } = string.Empty;

        // The folder name Payment is also a namespace, so the entity needs an alias here
        public Payment CurrentOrder { get; private set; }

        // The booking this payment belongs to, for the "back to your booking" link
        public string ReservationCode { get; private set; } = string.Empty;

        public string RestaurantSlug { get; private set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(OrderId))
                return NotFound();

            // Confirms with Cashfree and updates the stored status
            CurrentOrder = await _payments.GetOrderStatusAsync(OrderId, ct);
            if (CurrentOrder is null)
                return NotFound();

            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Reservation)
                .Include(o => o.Restaurant)
                .FirstOrDefaultAsync(o => o.Id == CurrentOrder.OrderId, ct);

            ReservationCode = order?.Reservation?.Code ?? string.Empty;
            RestaurantSlug = order?.Restaurant?.Slug ?? string.Empty;

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
