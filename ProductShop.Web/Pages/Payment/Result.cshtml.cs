using Microsoft.AspNetCore.Mvc;
using ProductShop.Application.Services.Interface;
using ProductShop.Domain.Entities;
using ProductShop.Domain.Enum;
using ProductShop.Web.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace ProductShop.Web.Pages.Payment
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

        public Orders CurrentOrder { get; private set; }

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
                case OrderStatus.Paid:
                    ToastSuccess("Payment successful. Thank you for your order.");
                    break;
                case OrderStatus.Failed:
                    ToastError("Payment was not completed. You can try again.");
                    break;
            }

            return Page();
        }
    }
}
