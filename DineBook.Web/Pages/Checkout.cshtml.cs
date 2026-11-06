using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DineBook.Application.Configuration;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Infrastructure.Database;
using DineBook.Web.Infrastructure;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages
{
    [EnableRateLimiting("checkout")]
    public class CheckoutModel : AppPageModel
    {
        private const string PendingKey = "PendingPayment";

        // Cashfree payment sessions expire. Confirm the lifetime in the Cashfree docs.
        private static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(30);

        private readonly ApplicationDbContext _db;
        private readonly IPaymentService _payments;
        private readonly IConfiguration _configuration;
        private readonly SiteOptions _site;
        private readonly ILogger<CheckoutModel> _logger;

        public CheckoutModel(
            ApplicationDbContext db,
            IPaymentService payments,
            IConfiguration configuration,
            IOptions<SiteOptions> site,
            ILogger<CheckoutModel> logger)
        {
            _db = db;
            _payments = payments;
            _configuration = configuration;
            _site = site.Value;
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

        // One key per checkout form. A double submit or a "back" resubmit sends the same key,
        // so the customer gets the same payment instead of a new one.
        [BindProperty]
        public string IdempotencyKey { get; set; } = string.Empty;

        // The food order being paid. Its items, categories and prices are shown as the bill.
        public Orders Order { get; private set; }

        public string ReservationCode { get; private set; } = string.Empty;

        public string RestaurantName { get; private set; } = string.Empty;

        // Null when there is no live payment session for this order
        public PendingPayment Pending { get; private set; }

        public string CashfreeMode =>
            string.Equals(_configuration["Cashfree:Environment"], "production", StringComparison.OrdinalIgnoreCase)
                ? "production"
                : "sandbox";

        public async Task<IActionResult> OnGetAsync(long id, CancellationToken ct)
        {
            if (!await LoadOrderAsync(id, ct))
                return NotFound();

            if (Order.Status == OrderStatus.Cancelled || Order.IsPaid)
                return RedirectToSettled();

            Pending = ReadPending(id);
            IdempotencyKey = Guid.NewGuid().ToString("N");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long id, CancellationToken ct)
        {
            if (!await LoadOrderAsync(id, ct))
                return NotFound();

            if (Order.Status == OrderStatus.Cancelled || Order.IsPaid)
                return RedirectToSettled();

            if (!ModelState.IsValid)
            {
                ToastError("Please check your details and try again.");
                return Page();
            }

            if (string.IsNullOrWhiteSpace(IdempotencyKey))
                IdempotencyKey = Guid.NewGuid().ToString("N");

            try
            {
                // Built from configuration, never from the Host header
                var returnUrl = $"{_site.PublicBaseUrl.TrimEnd('/')}/Payment/Result";

                // The amount is read from the order inside PaymentService, never from the form
                var session = await _payments.CreateOrderAsync(
                    id, Email.Trim(), Phone.Trim(), returnUrl, IdempotencyKey, ct);

                if (session is null)
                    return NotFound();

                var pending = new PendingPayment(
                    id, session.OrderId, session.PaymentSessionId, session.Amount, DateTimeOffset.UtcNow);

                // Post/Redirect/Get: a refresh shows this session instead of creating another payment
                TempData[PendingKey] = JsonSerializer.Serialize(pending);
                return RedirectToPage(new { id });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Log the error, but never the email or phone
                _logger.LogError(ex, "Failed to start payment for order {OrderId}", id);
                ToastError("Payment could not be started. Please try again.");
                return Page();
            }
        }

        private PendingPayment ReadPending(long orderId)
        {
            if (TempData.Peek(PendingKey) is not string json)
                return null;

            var pending = JsonSerializer.Deserialize<PendingPayment>(json);
            if (pending is null || pending.FoodOrderId != orderId)
                return null;

            if (DateTimeOffset.UtcNow - pending.CreatedUtc > SessionLifetime)
                return null;

            return pending;
        }

        private async Task<bool> LoadOrderAsync(long id, CancellationToken ct)
        {
            Order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
                .Include(o => o.Payments)
                .Include(o => o.Restaurant)
                .Include(o => o.Reservation)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (Order is null)
                return false;

            ReservationCode = Order.Reservation?.Code ?? string.Empty;
            RestaurantName = Order.Restaurant?.Name ?? string.Empty;
            return true;
        }

        // A paid or cancelled order needs no checkout: show the booking instead
        private RedirectToPageResult RedirectToSettled()
        {
            ToastSuccess("This order has already been paid or cancelled.");
            return string.IsNullOrEmpty(ReservationCode)
                ? RedirectToPage("/Index")
                : RedirectToPage("/Reservations/Details", new { code = ReservationCode });
        }
    }

    public sealed record PendingPayment(
        long FoodOrderId,
        string OrderId,
        string SessionId,
        decimal Amount,
        DateTimeOffset CreatedUtc);
}
