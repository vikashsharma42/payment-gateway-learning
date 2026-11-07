using DineBook.Application.Configuration;
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
                .Include(r => r.Tables)
                .OrderBy(r => r.Name)
                .ToListAsync(ct);
        }

        public Task<Restaurant> GetRestaurantAsync(long restaurantId, CancellationToken ct = default) =>
            _db.Restaurants.AsNoTracking()
                .Include(r => r.Tables)
                .FirstOrDefaultAsync(r => r.Id == restaurantId, ct);

        public async Task<IReadOnlyList<DateTime>> GetAvailableSlotsAsync(
            long restaurantId, DateOnly date, int guests, CancellationToken ct = default)
        {
            var policy = _options.ToPolicy();
            if (guests < 1 || guests > policy.MaxGuests)
                throw new DomainException($"Party size must be between 1 and {policy.MaxGuests} guests.");

            var restaurant = await GetRestaurantAsync(restaurantId, ct)
                ?? throw new DomainException("Restaurant not found.");

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

                if (ReservationRules.PickTable(restaurant.Tables, existing, guests, start, policy.DurationMinutes) is not null)
                    slots.Add(start);
            }

            return slots;
        }

        public async Task<Reservation> BookAsync(BookingRequest request, CancellationToken ct = default)
        {
            var policy = _options.ToPolicy();
            var restaurant = await GetRestaurantAsync(request.RestaurantId, ct)
                ?? throw new DomainException("Restaurant not found.");

            ReservationRules.EnsureBookable(restaurant, policy, request.Guests, request.StartsAt, LocalNow());

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

                var table = ReservationRules.PickTable(
                    restaurant.Tables,
                    existing,
                    request.Guests,
                    request.StartsAt,
                    policy.DurationMinutes)
                    ?? throw new DomainException("No table is free at that time for this party size.");

                var newReservation = new Reservation(
                    await NewCodeAsync(ct),
                    restaurant.Id,
                    table.Id,
                    request.CustomerName,
                    request.CustomerPhone,
                    request.CustomerEmail,
                    request.Guests,
                    request.StartsAt,
                    policy.DurationMinutes,
                    request.SpecialRequests);

                var customer = await FindOrCreateCustomerAsync(
                    newReservation, ct);

                customer.AddReservation(newReservation);

                _db.Reservations.Add(newReservation);

                await _db.SaveChangesAsync(ct);

                if (transaction is not null)
                    await transaction.CommitAsync(ct);

                reservation = newReservation;
            });

            // Sent only after the booking is saved
            await SendSafelyAsync(new ReservationConfirmedEmailModel(
                reservation.CustomerEmail,
                reservation.CustomerName,
                restaurant.Name,
                reservation.Code,
                reservation.StartsAt,
                reservation.Guests), ct);

            return reservation;
        }

        public async Task<IReadOnlyList<Reservation>> GetBookingsAsync(
            IReadOnlyCollection<string> codes, string accountPhone, CancellationToken ct = default)
        {
            var normalizedCodes = (codes ?? Array.Empty<string>())
                .Select(c => (c ?? string.Empty).Trim().ToUpperInvariant())
                .Where(c => c.Length > 0)
                .Distinct()
                .ToList();

            var phone = Reservation.NormalizePhone(accountPhone);
            if (normalizedCodes.Count == 0 && phone.Length == 0)
                return Array.Empty<Reservation>();

            return await _db.Reservations.AsNoTracking()
                .Include(r => r.Restaurant)
                .Where(r => normalizedCodes.Contains(r.Code) || (phone.Length > 0 && r.CustomerPhone == phone))
                .OrderByDescending(r => r.StartsAt)
                .ToListAsync(ct);
        }

        public Task<Reservation> FindByCodeAsync(string code, CancellationToken ct = default)
        {
            var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
            return _db.Reservations.AsNoTracking().FirstOrDefaultAsync(r => r.Code == normalized, ct);
        }

        public async Task<Reservation> CancelAsync(string code, string phone, CancellationToken ct = default)
        {
            var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
            var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Code == normalized, ct)
                ?? throw new DomainException("We could not find a booking with that code.");

            // A booking with a paid food order is final: the money has been taken for food served on it
            var hasPaidFood = await _db.Orders.AsNoTracking()
                .Where(o => o.ReservationId == reservation.Id)
                .SelectMany(o => o.Payments)
                .AnyAsync(p => p.Status == PaymentStatus.Paid, ct);

            if (hasPaidFood)
                throw new DomainException("This booking has a paid food order and cannot be cancelled.");

            reservation.Cancel(phone);
            await _db.SaveChangesAsync(ct);

            var restaurantName = await _db.Restaurants.AsNoTracking()
                .Where(r => r.Id == reservation.RestaurantId)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(ct);

            await SendSafelyAsync(new ReservationCancelledEmailModel(
                reservation.CustomerEmail,
                reservation.CustomerName,
                restaurantName ?? "your restaurant",
                reservation.Code,
                reservation.StartsAt), ct);

            return reservation;
        }

        // Guests are matched by phone number. The booking keeps its own copy of the contact details.
        private async Task<Customer> FindOrCreateCustomerAsync(Reservation reservation, CancellationToken ct)
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Phone == reservation.CustomerPhone, ct);

            if (customer is null)
            {
                customer = new Customer(reservation.CustomerName, reservation.CustomerPhone, reservation.CustomerEmail);
                _db.Customers.Add(customer);
            }
            else
            {
                customer.UpdateContact(reservation.CustomerName, reservation.CustomerEmail);
            }

            return customer;
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