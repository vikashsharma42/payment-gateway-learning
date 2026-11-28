using DineBook.Application.Configuration;
using DineBook.Application.Paging;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;
using DineBook.Email;
using DineBook.Email.EmailModels;
using DineBook.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services
{
    public sealed class ReservationService : IReservationService
    {
        // Easy to read aloud: no 0/O or 1/I
        private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const int CodeLength = 8;

        // Longest booking. Reservations that start up to this long before a slot can still overlap it.
        private static readonly TimeSpan LookBack = TimeSpan.FromHours(5);

        private readonly ApplicationDbContext _db;
        private readonly DiningOptions _options;
        private readonly TimeZoneInfo _zone;
        private readonly IEmailService _email;
        private readonly ILogger<ReservationService> _logger;

        public ReservationService(
            ApplicationDbContext db,
            IOptions<DiningOptions> options,
            IEmailService email,
            ILogger<ReservationService> logger)
        {
            _db = db;
            _options = options.Value;
            _zone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);
            _email = email;
            _logger = logger;
        }

        public DateTime LocalNow() => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _zone).DateTime;

        public async Task<IReadOnlyList<Restaurant>> GetRestaurantsAsync(CancellationToken ct = default)
        {
            return await _db.Restaurants.AsNoTracking()
                .Include(r => r.Tables).Include(r => r.Locations)
                .OrderBy(r => r.Name)
                .ToListAsync(ct);
        }

        public Task<PagedResult<Restaurant>> GetRestaurantsPageAsync(int page, int pageSize, bool openOnly, CancellationToken ct = default)
        {
            var query = _db.Restaurants.AsNoTracking()
                .Include(r => r.Tables).Include(r => r.Locations)
                .AsSplitQuery()
                .AsQueryable();

            // The public directory lists only restaurants with an open location. The admin list shows all of them.
            if (openOnly)
                query = query.Where(r => r.IsActive && r.Locations.Any(l => l.IsActive));

            return query
                .OrderBy(r => r.Name)
                .ThenBy(r => r.Id)
                .ToPagedAsync(page, pageSize, ct);
        }

        public Task<Restaurant> GetRestaurantAsync(long restaurantId, CancellationToken ct = default) =>
            _db.Restaurants.AsNoTracking()
                .Include(r => r.Tables).Include(r => r.Locations)
                .FirstOrDefaultAsync(r => r.Id == restaurantId, ct);

        public Task<Restaurant> GetRestaurantBySlugAsync(string slug, CancellationToken ct = default) =>
            _db.Restaurants
            .AsNoTracking()
            .Include(r => r.Tables)
            .Include(r => r.Locations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Slug == slug, ct);

        // A location can only be booked while it is open
        private static void EnsureLocationOpen(Restaurant restaurant, long? locationId)
        {
            if (locationId is null)
                return;

            if (!restaurant.ActiveLocations.Any(l => l.Id == locationId.Value))
                throw new DomainException("This location is not open for bookings.");
        }

        // Tables of the chosen location, or every table of the restaurant when no location is chosen
        private static IEnumerable<Table> TablesFor(Restaurant restaurant, long? locationId) =>
            locationId is null
                ? restaurant.Tables
                : restaurant.Tables.Where(t => t.LocationId == locationId.Value);

        public async Task<IReadOnlyList<DateTime>> GetAvailableSlotsAsync(long restaurantId, DateOnly date, int guests, long? locationId = null, CancellationToken ct = default)
        {
            var policy = _options.ToPolicy();
            if (guests < 1 || guests > policy.MaxGuests)
                throw new DomainException($"Please choose between 1 and {policy.MaxGuests} guests for your party.");

            var restaurant = await GetRestaurantAsync(restaurantId, ct)
                ?? throw new DomainException("Restaurant not found.");

            EnsureLocationOpen(restaurant, locationId);
            var tables = TablesFor(restaurant, locationId);

            var now = LocalNow();
            var slots = new List<DateTime>();

            // Days outside the booking window have no slots
            if (date < DateOnly.FromDateTime(now) || date > DateOnly.FromDateTime(now).AddDays(policy.MaxAdvanceDays))
                return slots;

            var dayStart = date.ToDateTime(TimeOnly.MinValue);
            var dayEnd = dayStart.AddDays(1);
            var existing = await LoadBlockingAsync(restaurantId, dayStart - LookBack, dayEnd, ct);

            var duration = TimeSpan.FromMinutes(policy.DurationMinutes);
            var step = TimeSpan.FromMinutes(policy.SlotMinutes);
            var close = restaurant.ClosesAt.ToTimeSpan();

            for (var offset = restaurant.OpensAt.ToTimeSpan(); offset + duration <= close; offset += step)
            {
                var start = dayStart + offset;
                if (start < now.AddMinutes(policy.MinNoticeMinutes))
                    continue;

                if (ReservationRules.PickTable(tables, existing, guests, start, policy.DurationMinutes) is not null)
                    slots.Add(start);
            }

            return slots;
        }

        public async Task<Reservation> BookAsync(BookingRequest request, CancellationToken ct = default)
        {
            var policy = _options.ToPolicy();
            var restaurant = await GetRestaurantAsync(request.RestaurantId, ct)
                ?? throw new DomainException("Restaurant not found.");

            var customer = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.CustomerId, ct)
                ?? throw new DomainException("Account not found. Please sign in again.");

            ReservationRules.EnsureBookable(restaurant, policy, request.Guests, request.StartsAt, LocalNow());
            EnsureLocationOpen(restaurant, request.LocationId);
            var tables = TablesFor(restaurant, request.LocationId);

            Reservation reservation = null;

            // SQL Server retry execution strategy
            var strategy = _db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                // Transaction must be created inside the execution strategy.
                await using var transaction = _db.Database.IsRelational()
                    ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
                    : null;

                var existing = await LoadBlockingAsync(
                    restaurant.Id,
                    request.StartsAt - LookBack,
                    request.StartsAt.AddMinutes(policy.DurationMinutes),
                    ct);

                // One booking per time for this guest. Overlapping bookings at any location of this restaurant are refused too.
                var guestBookings = await _db.Reservations.AsNoTracking()
                    .Where(r => r.ApplicationUserId == customer.Id
                                && r.RestaurantId == restaurant.Id
                                && (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Seated)
                                && r.StartsAt >= request.StartsAt - LookBack
                                && r.StartsAt < request.StartsAt.AddMinutes(policy.DurationMinutes))
                    .ToListAsync(ct);

                if (guestBookings.Any(r => ReservationRules.Overlaps(r.StartsAt, r.DurationMinutes, request.StartsAt, policy.DurationMinutes)))
                    throw new DomainException("You already have a booking at this time. Check your bookings before booking again.");

                var table = ReservationRules.PickTable(
                    tables,
                    existing,
                    request.Guests,
                    request.StartsAt,
                    policy.DurationMinutes)
                    ?? throw new DomainException("Sorry, no table is free at that time for your party size. Please try another time.");

                var newReservation = new Reservation(
                    await NewCodeAsync(ct),
                    customer,
                    request.Guests,
                    request.StartsAt,
                    policy.DurationMinutes,
                    request.SpecialRequests);

                // The restaurant checks the table and that the customer is registered with it
                restaurant.AddReservation(table.Id, newReservation);

                _db.Reservations.Add(newReservation);

                await _db.SaveChangesAsync(ct);

                if (transaction is not null)
                    await transaction.CommitAsync(ct);

                reservation = newReservation;
            });

            // Sent only after the booking is saved
            await SendSafelyAsync(
                new ReservationConfirmedEmailModel(
                    reservation.CustomerEmail,
                    reservation.CustomerName,
                    restaurant.Name,
                    reservation.Code,
                    reservation.StartsAt,
                    reservation.Guests),
                ct);

            return reservation;
        }

        public Task<Reservation> FindByCodeAsync(string code, CancellationToken ct = default)
        {
            var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
            return _db.Reservations.AsNoTracking().IgnoreQueryFilters().Include(r => r.Table).FirstOrDefaultAsync(r => r.Code == normalized, ct);
        }

        public async Task<Reservation> CancelAsync(string code, string phone, CancellationToken ct = default)
        {
            var normalizedCode = (code ?? string.Empty).Trim().ToUpperInvariant();
            var normalizedPhone = ApplicationUser.NormalizePhone(phone);

            var reservation = await _db.Reservations.Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.Code == normalizedCode, ct)
                ?? throw new DomainException("We couldn't find a booking with that code and phone number. Please check them and try again.");

            // Same message for an unknown code and a wrong phone
            if (reservation.Customer?.Phone != normalizedPhone)
                throw new DomainException("We couldn't find a booking with that code and phone number. Please check them and try again.");

            reservation.CancelByCustomer(LocalNow());
            await _db.SaveChangesAsync(ct);

            var restaurantName = await _db.Restaurants.AsNoTracking()
                .Where(r => r.Id == reservation.RestaurantId)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(ct);

            // Sent only after the cancellation is saved
            await SendSafelyAsync(
                new ReservationCancelledEmailModel(
                    reservation.CustomerEmail,
                    reservation.CustomerName,
                    restaurantName ?? "your restaurant",
                    reservation.Code,
                    reservation.StartsAt),
                ct);

            return reservation;
        }

        // Customer changes the party size and/or special requests. Same phone check as cancel.
        // A new party size picks a free table in the same location. The check and save run in a serializable transaction.
        public async Task<Reservation> UpdateBookingAsync(string code, string phone, int guests, string specialRequests, CancellationToken ct = default)
        {
            var policy = _options.ToPolicy();
            var normalizedCode = (code ?? string.Empty).Trim().ToUpperInvariant();
            var normalizedPhone = ApplicationUser.NormalizePhone(phone);
            var now = LocalNow();

            var reservation = await _db.Reservations.Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.Code == normalizedCode, ct)
                ?? throw new DomainException("We couldn't find a booking with that code and phone number. Please check them and try again.");

            // Same message for an unknown code and a wrong phone
            if (reservation.Customer?.Phone != normalizedPhone)
                throw new DomainException("We couldn't find a booking with that code and phone number. Please check them and try again.");

            var requests = (specialRequests ?? string.Empty).Trim();
            var partyChanged = guests != reservation.Guests;
            var requestsChanged = !string.Equals(requests, reservation.SpecialRequests, StringComparison.Ordinal);

            // Nothing changed: nothing to save
            if (!partyChanged && !requestsChanged)
                return reservation;

            reservation.EnsureCustomerCanChange(now);

            if (partyChanged && (guests < 1 || guests > policy.MaxGuests))
                throw new DomainException($"Please choose between 1 and {policy.MaxGuests} guests for your party.");

            var restaurant = await GetRestaurantAsync(reservation.RestaurantId, ct)
                ?? throw new DomainException("Restaurant not found.");

            // The booking stays in the location of its current table
            var currentTable = restaurant.Tables.FirstOrDefault(t => t.Id == reservation.TableId);
            var tables = TablesFor(restaurant, currentTable?.LocationId);

            var strategy = _db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = _db.Database.IsRelational()
                    ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
                    : null;

                if (partyChanged)
                {
                    var existing = await LoadBlockingAsync(
                        restaurant.Id,
                        reservation.StartsAt - LookBack,
                        reservation.EndsAt,
                        ct);

                    // The booking's own table must not count as taken by itself
                    var others = existing.Where(r => r.Id != reservation.Id).ToList();

                    var table = ReservationRules.PickTable(tables, others, guests, reservation.StartsAt, reservation.DurationMinutes)
                        ?? throw new DomainException("Sorry, no table is free at this time for that party size. Please try another time or call the restaurant.");

                    reservation.ChangeParty(guests, table, now);
                }

                if (requestsChanged)
                    reservation.ChangeSpecialRequests(requests, now);

                await _db.SaveChangesAsync(ct);

                if (transaction is not null)
                    await transaction.CommitAsync(ct);
            });

            return reservation;
        }

        public Task SeatAsync(long restaurantId, long reservationId, CancellationToken ct = default) =>
            ChangeStatusAsync(restaurantId, reservationId, r =>
            {
                r.Seat();
                r.Table.Occupy();
            }, ct);

        public Task CompleteAsync(long restaurantId, long reservationId, CancellationToken ct = default) =>
            ChangeStatusAsync(restaurantId, reservationId, r =>
            {
                r.Complete();
                r.Table.Release();
            }, ct);

        public Task MarkNoShowAsync(long restaurantId, long reservationId, CancellationToken ct = default) =>
            ChangeStatusAsync(restaurantId, reservationId, r => r.MarkNoShow(), ct);

        private async Task ChangeStatusAsync(long restaurantId, long reservationId, Action<Reservation> change, CancellationToken ct)
        {
            // Tables removed since the booking are still needed to seat, release or close it
            var reservation = await _db.Reservations.IgnoreQueryFilters().Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.RestaurantId == restaurantId, ct)
                ?? throw new DomainException("Booking not found.");

            change(reservation);
            await _db.SaveChangesAsync(ct);
        }

        private Task<List<Reservation>> LoadBlockingAsync(
            long restaurantId, DateTime from, DateTime to, CancellationToken ct) =>
            _db.Reservations
                .Where(r => r.RestaurantId == restaurantId
                            && r.StartsAt >= from
                            && r.StartsAt < to
                            && (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Seated))
                .ToListAsync(ct);

        private async Task<string> NewCodeAsync(CancellationToken ct)
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                var chars = new char[CodeLength];
                for (var i = 0; i < CodeLength; i++)
                {
                    chars[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
                }

                var code = new string(chars);
                if (!await _db.Reservations.AnyAsync(r => r.Code == code, ct))
                    return code;
            }

            throw new DomainException("Could not create a booking code. Please try again.");
        }

        private async Task SendSafelyAsync(BaseEmailModel message, CancellationToken ct)
        {
            try
            {
                await _email.SendAsync(message, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A failed email must never undo a booking or cancellation that is already saved
                _logger.LogWarning(ex, "Email '{Subject}' could not be sent", message.Subject);
            }
        }
    }
}
