using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DineBook.Application.Services.Interface;
using PaymentRecord = DineBook.Domain.Entities.Payment;
using DineBook.Domain.Enums;
using DineBook.Web.Infrastructure;

namespace DineBook.Web.Pages.Payment
{
    public class ResultModel : AppPageModel
    {
        private readonly IPaymentService _payments;

        public ResultModel(IPaymentService payments)
        {
            _payments = payments;
        }

        // Cashfree sends the order id as "order_id" in the query string
        [BindProperty(SupportsGet = true, Name = "order_id")]
        public string OrderId { get; set; } = string.Empty;

        // The folder name Payment is also a namespace, so the entity needs an alias here
        public PaymentRecord CurrentOrder { get; private set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(OrderId))
                return RedirectToPage("/Index");

            // Confirms with Cashfree and updates the stored status
            CurrentOrder = await _payments.GetOrderStatusAsync(OrderId, ct);
            if (CurrentOrder is null)
                return NotFound();

            switch (CurrentOrder.Status)
            {
                case PaymentStatus.Paid:
                    ToastSuccess("Payment successful. Thank you for your order.");
                    break;
                case PaymentStatus.Failed:
                    ToastError("Payment was not completed. You can try again.");
                    break;
            }

            return Page();
        }
    }
}
