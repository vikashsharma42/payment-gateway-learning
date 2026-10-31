using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProductShop.Application.Services.Interface;
using ProductShop.Domain.Entities;
using ProductShop.Infrastructure.Database;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace ProductShop.Web.Pages
{
    public class CheckoutModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IPaymentService _payments;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CheckoutModel> _logger;

        public CheckoutModel(ApplicationDbContext db, IPaymentService payments, IConfiguration configuration, ILogger<CheckoutModel> logger)
        {
            _db = db;
            _payments = payments;
            _configuration = configuration;
            _logger = logger;
        }

        [BindProperty]
        [Required, EmailAddress]
        public string Email { get; set; }

        [BindProperty]
        [Required, StringLength(15, MinimumLength = 10)]
        public string Phone { get; set; }

        public Product Product { get; private set; }

        public string OrderId { get; private set; }

        public string PaymentSessionId { get; private set; }

        public decimal Amount { get; private set; }

        public string ErrorMessage { get; private set; }

        // Cashfree JS SDK mode: "sandbox" or "production"
        public string CashfreeMode => _configuration["Cashfree:Environment"] ?? "sandbox";

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
        {
            if (!await LoadProductAsync(id, ct))
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id, CancellationToken ct)
        {
            if (!await LoadProductAsync(id, ct))
                return NotFound();

            if (!ModelState.IsValid)
                return Page();

            try
            {
                // Cashfree redirects here after payment, with ?order_id=...
                var returnUrl = Url.Page("/Payment/Result", null, null, Request.Scheme)!;

                // Price is read from the database inside PaymentService, never from the form
                var session = await _payments.CreateOrderAsync(id, Email, Phone, returnUrl, ct);
                if (session is null)
                    return NotFound();

                OrderId = session.OrderId;
                PaymentSessionId = session.PaymentSessionId;
                Amount = session.Amount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start payment for product {ProductId}", id);
                ErrorMessage = "Payment could not be started. Please try again.";
            }

            return Page();
        }

        private async Task<bool> LoadProductAsync(int id, CancellationToken ct)
        {
            var product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            if (product is null)
                return false;

            Product = product;
            return true;
        }
    }
}