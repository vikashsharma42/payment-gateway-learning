using DineBook.Application.Configuration;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Infrastructure.Database;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
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
        private readonly BookingAccess _access;

        public CheckoutModel(
            ApplicationDbContext db,
            IPaymentService payments,
            IConfiguration configuration,
            IOptions<SiteOptions> site,
            BookingAccess access)
        {
            _db = db;
            _payments = payments;
            _configuration = configuration;
            _site = site.Value;
            _access = access;
        }

        [BindProperty]
        public string IdempotencyKey { get; set; } = string.Empty;

        // The food order being paid
        public Domain.Entities.Orders Order { get; private set; }

        // The booking the bill belongs to. Its name, phone and email are used for the payment.
        public Reservation Booking { get; private set; }

        public string RestaurantName { get; private set; } = string.Empty;

        public string ReservationCode => Booking?.Code ?? string.Empty;

        // Null when there is no live payment session for this order
        public PendingPayment Pending { get; private set; }

        public string CashfreeMode =>
            string.Equals(_configuration["Cashfree:Environment"], "production", StringComparison.OrdinalIgnoreCase)
                ? "production"
                : "sandbox";

        public async Task<IActionResult> OnGetAsync(long id, CancellationToken ct)
        {
            if (!await LoadAsync(id, ct))
                return NotFound();

            if (Order.Status == OrderStatus.Cancelled || Order.IsPaid)
                return RedirectToSettled();

            Pending = ReadPending(id);
            IdempotencyKey = Guid.NewGuid().ToString("N");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long id, CancellationToken ct)
        {
            if (!await LoadAsync(id, ct))
                return NotFound();

            if (Order.Status == OrderStatus.Cancelled || Order.IsPaid)
                return RedirectToSettled();

            if (string.IsNullOrWhiteSpace(IdempotencyKey))
                IdempotencyKey = Guid.NewGuid().ToString("N");

            // Built from configuration, never from the Host header
            var returnUrl = $"{_site.PublicBaseUrl.TrimEnd('/')}/Payment/Result";

            // Contact details come from the booking, so nobody types them again.
            // The amount is read from the order inside PaymentService, never from the form.
            var session = await _payments.CreateOrderAsync(
                id, Booking.CustomerEmail, Booking.CustomerPhone, returnUrl, IdempotencyKey, ct);

            if (session is null)
                return NotFound();

            var pending = new PendingPayment(
                id, session.OrderId, session.PaymentSessionId, session.Amount, DateTimeOffset.UtcNow);

            // Post/Redirect/Get: a refresh shows this session instead of creating another payment
            TempData[PendingKey] = JsonSerializer.Serialize(pending);
            return RedirectToPage(new { id });
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

        // Loads the bill and checks the visitor owns its booking. Someone else's bill looks like "not found".
        private async Task<bool> LoadAsync(long id, CancellationToken ct)
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

            Booking = Order.Reservation;
            RestaurantName = Order.Restaurant?.Name ?? string.Empty;

            return Booking is not null && await _access.OwnsAsync(Booking, ct);
        }

        // A paid or cancelled order needs no checkout: show the booking instead
        private RedirectToPageResult RedirectToSettled()
        {
            ToastSuccess("This order has already been paid or cancelled.");
            return string.IsNullOrEmpty(ReservationCode)
                ? RedirectToPage("/Bookings/Index")
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
