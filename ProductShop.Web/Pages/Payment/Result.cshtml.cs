using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProductShop.Application.Services.Interface;
using ProductShop.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace ProductShop.Web.Pages.Payment
{
    public class ResultModel : PageModel
    {
        private readonly IPaymentService _payments;

        public ResultModel(IPaymentService payments)
        {
            _payments = payments;
        }

        // Cashfree sends the order id as "order_id" in the query string
        [BindProperty(SupportsGet = true, Name = "order_id")]
        public string OrderId { get; set; }

        public Order CurrentOrder { get; private set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(OrderId))
                return RedirectToPage("/Index");

            // Confirms with Cashfree and updates the order status in the database
            CurrentOrder = await _payments.GetOrderStatusAsync(OrderId, ct);
            if (CurrentOrder is null)
                return NotFound();

            return Page();
        }
    }
}