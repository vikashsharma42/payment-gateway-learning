using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DineBook.Application.Configuration;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;
using DineBook.Web.Dtos;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DineBook.Web.Pages.Reservations
{
    // /{slug}/Reservations/{code}/Deposit: the customer pays the booking deposit that secures the table.
    // Only customers of this restaurant can pay it, and the amount comes from the server, never the form.
    [EnableRateLimiting("checkout")]
    public class DepositModel : AppPageModel
    {
        private const string PendingKey = "PendingDeposit";

        // Cashfree payment sessions expire. Same lifetime as food checkout.
        private static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(30);

        private readonly IReservationService _reservations;
        private readonly IPaymentService _payments;
        private readonly ICustomerService _customers;
        private readonly IConfiguration _configuration;
        private readonly SiteOptions _site;
        private readonly ILogger<DepositModel> _logger;

        public DepositModel(
            IReservationService reservations,
            IPaymentService payments,
            ICustomerService customers,
            IConfiguration configuration,
            IOptions<SiteOptions> site,
            ILogger<DepositModel> logger)
        {
            _reservations = reservations;
            _payments = payments;
            _customers = customers;
            _configuration = configuration;
            _site = site.Value;
            _logger = logger;
        }

        [FromRoute]
        public string Slug { get; set; } = string.Empty;

        [FromRoute]
        public string Code { get; set; } = string.Empty;

        [FromRoute]
        public string LocationSlug { get; set; } = string.Empty;

        // Email, phone and the idempotency key (see CheckoutDto)
        [BindProperty]
        public CheckoutDto Input { get; set; } = new();

        public Restaurant Restaurant { get; private set; }

        public Reservation Reservation { get; private set; }

        public decimal Amount => _payments.BookingAdvance;

        public decimal CancellationFee => _payments.CancellationFee;

        // What is kept when the booking is cancelled and what comes back
        public decimal Refund => Amount - CancellationFee;

        // Null when there is no live payment session for this deposit
        public PendingDeposit Pending { get; private set; }

        public string CashfreeMode =>
            string.Equals(_configuration["Cashfree:Environment"], "production", StringComparison.OrdinalIgnoreCase)
                ? "production"
                : "sandbox";

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            if (!await LoadAsync(ct))
                return NotFound();

            // The booking's own location decides its address: a wrong location in the URL is corrected
            var bookingLocation = Restaurant.LocationSlugFor(Reservation);
            if (!string.IsNullOrEmpty(bookingLocation)
                && !string.Equals(bookingLocation, LocationSlug, StringComparison.OrdinalIgnoreCase))
                return RedirectToPage(new { slug = Slug, locationSlug = bookingLocation, code = Code });

            if (!IsCustomerOf(Restaurant.Id))
                return RedirectToPage("/Restaurants/Login", new { slug = Slug, ReturnUrl = Request.Path.ToString() });

            if (!IsPayable())
                return RedirectToPage("/Reservations/Details", new { slug = Slug, locationSlug = LocationSlug, code = Code });

            Pending = ReadPending();
            Input.IdempotencyKey = Guid.NewGuid().ToString("N");
            await PrefillFromAccountAsync(ct);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!await LoadAsync(ct))
                return NotFound();

            if (!IsCustomerOf(Restaurant.Id))
                return RedirectToPage("/Restaurants/Login", new { slug = Slug, ReturnUrl = Request.Path.ToString() });

            if (!IsPayable())
                return RedirectToPage("/Reservations/Details", new { slug = Slug, locationSlug = LocationSlug, code = Code });

            if (!ModelState.IsValid)
            {
                ToastError("Please check your details and try again.");
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Input.IdempotencyKey))
                Input.IdempotencyKey = Guid.NewGuid().ToString("N");

            try
            {
                var returnUrl = $"{_site.PublicBaseUrl.TrimEnd('/')}/Payment/Result";
                var session = await _payments.CreateDepositAsync(
                    Code, Input.Email.Trim(), DineBook.Domain.ValueObjects.PhoneNumbers.Resolve(Input.CountryCode, Input.Phone), returnUrl, Input.IdempotencyKey, ct);

                var pending = new PendingDeposit(session.OrderId, session.PaymentSessionId, session.Amount, DateTimeOffset.UtcNow);

                // Post/Redirect/Get: a refresh shows this session instead of creating another payment
                TempData[PendingKey] = JsonSerializer.Serialize(pending);
                return RedirectToPage(new { slug = Slug, locationSlug = LocationSlug, code = Code });
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
                return Page();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Log the error, but never the email or phone
                _logger.LogError(ex, "Failed to start deposit payment for booking {Code}", Code);
                ToastError("We couldn't start your payment. Please try again in a moment.");
                return Page();
            }
        }

        // The restaurant in the address, and a booking that belongs to it
        private async Task<bool> LoadAsync(CancellationToken ct)
        {
            Restaurant = await _reservations.GetRestaurantBySlugAsync(Slug, ct);
            if (Restaurant is null || !Restaurant.IsActive)
                return false;

            Reservation = await _reservations.FindByCodeAsync(Code, ct);
            return Reservation is not null && Reservation.RestaurantId == Restaurant.Id;
        }

        // Active bookings with no deposit paid yet. The payment itself is checked again on the server.
        private bool IsPayable() =>
            Reservation.Status != ReservationStatus.Cancelled
            && Reservation.Status != ReservationStatus.NoShow
            && Amount > 0m;

        private PendingDeposit ReadPending()
        {
            if (TempData.Peek(PendingKey) is not string json)
                return null;

            var pending = JsonSerializer.Deserialize<PendingDeposit>(json);
            if (pending is null)
                return null;

            return DateTimeOffset.UtcNow - pending.CreatedUtc > SessionLifetime ? null : pending;
        }

        // Fills the form from the account. CreateDepositAsync still checks the typed email and phone match it.
        private async Task PrefillFromAccountAsync(CancellationToken ct)
        {
            if (!string.IsNullOrEmpty(Input.Email) && !string.IsNullOrEmpty(Input.Phone))
                return;

            var account = await _customers.FindProfileAsync(UserId, ct);
            if (account is null)
                return;

            if (string.IsNullOrEmpty(Input.Email))
                Input.Email = account.Email;

            if (string.IsNullOrEmpty(Input.Phone))
            {
                Input.CountryCode = account.CountryCode;
                Input.Phone = DineBook.Domain.ValueObjects.PhoneNumbers.National(account.CountryCode, account.Phone);
            }
        }
    }

    public sealed record PendingDeposit(
        string OrderId,
        string SessionId,
        decimal Amount,
        DateTimeOffset CreatedUtc);
}
