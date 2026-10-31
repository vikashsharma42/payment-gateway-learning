using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProductShop.Application.Services.Interface;
using ProductShop.Domain.Entities;
using ProductShop.Infrastructure.Database;
using ProductShop.Web.Helpers;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProductShop.Web.Pages
{
    public class CheckoutModel : AppPageModel
    {
        private const string PendingKey = "PendingPayment";

        // Cashfree payment sessions expire. Confirm the lifetime in the Cashfree docs.
        private static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(30);

        private readonly ApplicationDbContext _db;
        private readonly IPaymentService _payments;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CheckoutModel> _logger;

        public CheckoutModel(
            ApplicationDbContext db,
            IPaymentService payments,
            IConfiguration configuration,
            ILogger<CheckoutModel> logger)
        {
            _db = db;
            _payments = payments;
            _configuration = configuration;
            _logger = logger;
        }

        [BindProperty]
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Phone is required.")]
        [RegularExpression(@"^\+?[0-9 ]{10,15}$", ErrorMessage = "Enter a valid phone number.")]
        public string Phone { get; set; } = string.Empty;

        public Products Product { get; private set; }

        // Null when there is no live payment session for this product
        public PendingPayment Pending { get; private set; }

        public string CashfreeMode =>
            string.Equals(_configuration["Cashfree:Environment"], "production", StringComparison.OrdinalIgnoreCase)
                ? "production"
                : "sandbox";

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
        {
            if (!await LoadProductAsync(id, ct))
                return NotFound();

            Pending = ReadPending(id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id, CancellationToken ct)
        {
            if (!await LoadProductAsync(id, ct))
                return NotFound();

            if (!ModelState.IsValid)
            {
                ToastError("Please check your details and try again.");
                return Page();
            }

            try
            {
                var returnUrl = Url.Page("/Payment/Result", null, null, Request.Scheme);

                // Price is read from the database inside PaymentService, never from the form
                var session = await _payments.CreateOrderAsync(id, Email.Trim(), Phone.Trim(), returnUrl, ct);
                if (session is null)
                    return NotFound();

                var pending = new PendingPayment(
                    id, session.OrderId, session.PaymentSessionId, session.Amount, DateTimeOffset.UtcNow);

                // Post/Redirect/Get: a refresh shows this session instead of creating another order
                TempData[PendingKey] = JsonSerializer.Serialize(pending);
                return RedirectToPage(new { id });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Log the error, but never the email or phone
                _logger.LogError(ex, "Failed to start payment for product {ProductId}", id);
                ToastError("Payment could not be started. Please try again.");
                return Page();
            }
        }

        private PendingPayment ReadPending(int productId)
        {
            if (TempData.Peek(PendingKey) is not string json)
                return null;

            var pending = JsonSerializer.Deserialize<PendingPayment>(json);
            if (pending is null || pending.ProductId != productId)
                return null;

            if (DateTimeOffset.UtcNow - pending.CreatedUtc > SessionLifetime)
                return null;

            return pending;
        }

        private async Task<bool> LoadProductAsync(int id, CancellationToken ct)
        {
            Product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            return Product is not null;
        }
    }

    public sealed record PendingPayment(
        int ProductId,
        string OrderId,
        string SessionId,
        decimal Amount,
        DateTimeOffset CreatedUtc);
}