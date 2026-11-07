using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Infrastructure
{
    /// <summary>
    /// Decides which bookings a visitor may see, and remembers them so nobody types the same details twice.
    /// A signed-in customer owns every booking made with their phone number.
    /// A guest owns the bookings they made, or unlocked with the phone number, on this browser.
    /// The guest memory is an encrypted, signed cookie. It cannot be read or changed by the browser's scripts.
    /// </summary>
    public sealed class BookingAccess
    {
        private const string CookieName = "dinebook.guest";
        private const int MaxRememberedBookings = 20;
        private static readonly TimeSpan Lifetime = TimeSpan.FromDays(60);

        private readonly IHttpContextAccessor _http;
        private readonly IDataProtector _protector;
        private readonly ICustomerContactService _contacts;
        private readonly IReservationService _reservations;
        private readonly IFoodOrderService _orders;

        public BookingAccess(
            IHttpContextAccessor http,
            IDataProtectionProvider protection,
            ICustomerContactService contacts,
            IReservationService reservations,
            IFoodOrderService orders)
        {
            _http = http;
            _protector = protection.CreateProtector("DineBook.GuestBookings.v1");
            _contacts = contacts;
            _reservations = reservations;
            _orders = orders;
        }

        private HttpContext Context => _http.HttpContext ?? throw new InvalidOperationException("HTTP context is unavailable.");

        public bool IsSignedIn => Context.User.Identity?.IsAuthenticated == true;

        public IReadOnlyList<string> RememberedCodes() => Read()?.Codes ?? new List<string>();

        /// <summary>
        /// Contact details to fill into a form: the signed-in account first, then the last guest details on this browser.
        /// Null when nothing is known yet.
        /// </summary>
        public async Task<CustomerContact> PrefillAsync(CancellationToken ct = default)
        {
            var accountId = AccountId();
            if (accountId is not null)
            {
                var contact = await _contacts.GetForUserAsync(accountId.Value, ct);
                if (contact is not null)
                    return contact;
            }

            return Read()?.LastContact;
        }

        /// <summary>True when this visitor may see or change the booking.</summary>
        public async Task<bool> OwnsAsync(Reservation booking, CancellationToken ct = default)
        {
            if (booking is null)
                return false;

            if (RememberedCodes().Contains(booking.Code))
                return true;

            var accountPhone = await AccountPhoneAsync(ct);
            return !string.IsNullOrEmpty(accountPhone)
                   && Reservation.NormalizePhone(accountPhone) == booking.CustomerPhone;
        }

        /// <summary>
        /// Remembers a booking on this browser. saveContact fills the booking form next time.
        /// Unlocking someone else's booking should pass false, so their details are not saved as ours.
        /// </summary>
        public void Remember(Reservation booking, bool saveContact)
        {
            var memory = Read();
            var codes = new List<string> { booking.Code };
            if (memory is not null)
                codes.AddRange(memory.Codes.Where(c => c != booking.Code));

            var lastContact = saveContact
                ? new CustomerContact(booking.CustomerName, booking.CustomerPhone, booking.CustomerEmail)
                : memory?.LastContact;

            Write(new GuestMemory(codes.Take(MaxRememberedBookings).ToList(), lastContact));
        }

        /// <summary>Every booking this visitor owns, each with its food orders. Newest booking first.</summary>
        public async Task<IReadOnlyList<BookingWithOrders>> LoadMyBookingsAsync(CancellationToken ct = default)
        {
            var phone = await AccountPhoneAsync(ct);
            var bookings = await _reservations.GetBookingsAsync(RememberedCodes(), phone, ct);
            if (bookings.Count == 0)
                return Array.Empty<BookingWithOrders>();

            var orders = await _orders.GetOrdersForBookingsAsync(bookings.Select(b => b.Id).ToList(), ct);

            return bookings
                .Select(b => new BookingWithOrders(b, orders.Where(o => o.ReservationId == b.Id).ToList()))
                .ToList();
        }

        private long? AccountId()
        {
            if (!IsSignedIn)
                return null;

            return long.TryParse(Context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
        }

        private async Task<string> AccountPhoneAsync(CancellationToken ct)
        {
            var accountId = AccountId();
            if (accountId is null)
                return null;

            var contact = await _contacts.GetForUserAsync(accountId.Value, ct);
            return contact?.Phone;
        }

        private GuestMemory Read()
        {
            if (!Context.Request.Cookies.TryGetValue(CookieName, out var value) || string.IsNullOrEmpty(value))
                return null;

            try
            {
                return JsonSerializer.Deserialize<GuestMemory>(_protector.Unprotect(value));
            }
            catch (Exception ex) when (ex is CryptographicException or JsonException)
            {
                // Tampered, expired key or old format: treat as no memory
                return null;
            }
        }

        private void Write(GuestMemory memory)
        {
            Context.Response.Cookies.Append(CookieName, _protector.Protect(JsonSerializer.Serialize(memory)), new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.Add(Lifetime)
            });
        }

        private sealed record GuestMemory(List<string> Codes, CustomerContact LastContact);
    }

    /// <summary>A booking with its food orders, for the bookings and order history pages.</summary>
    public sealed record BookingWithOrders(Reservation Booking, IReadOnlyList<Orders> FoodOrders)
    {
        public IReadOnlyList<Orders> UnpaidOrders => FoodOrders.Where(o => !o.IsPaid && o.Status != OrderStatus.Cancelled).ToList();

        public decimal UnpaidTotal => UnpaidOrders.Sum(o => o.Total());
    }
}
