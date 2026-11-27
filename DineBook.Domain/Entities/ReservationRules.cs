using DineBook.Domain.Enums;
using DineBook.Domain.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DineBook.Domain.Entities
{
    // Pure booking rules. No database access, so they can be tested directly.
    public static class ReservationRules
    {
        // Customers can change or cancel a booking until this long before it starts
        public static readonly TimeSpan CustomerChangeCutoff = TimeSpan.FromHours(2);

        // Confirmed and seated bookings hold a table. Cancelled and no-shows do not.
        public static bool IsBlocking(ReservationStatus status) =>
            status == ReservationStatus.Confirmed || status == ReservationStatus.Seated;

        public static bool Overlaps(DateTime startA, int durationA, DateTime startB, int durationB) =>
            startA < startB.AddMinutes(durationB) && startB < startA.AddMinutes(durationA);

        public static void EnsureBookable(
            Restaurant restaurant,
            ReservationPolicy policy,
            int guests,
            DateTime startsAt,
            DateTime nowLocal)
        {
            DomainValidation<bool>.Ensure(restaurant.IsActive).IsTrue("Sorry, this restaurant isn't taking bookings right now.");
            DomainValidation<int>.Ensure(guests).IsBetween(1, policy.MaxGuests, $"Please choose between 1 and {policy.MaxGuests} guests for your party.");
            DomainValidation<DateTime>.Ensure(startsAt).IsGreaterThanOrEqualTo(nowLocal.AddMinutes(policy.MinNoticeMinutes), $"Please book at least {policy.MinNoticeMinutes} minutes ahead so we can get your table ready.");
            DomainValidation<DateTime>.Ensure(startsAt.Date, nameof(startsAt)).IsLessThanOrEqualTo(nowLocal.Date.AddDays(policy.MaxAdvanceDays), $"You can book up to {policy.MaxAdvanceDays} days ahead.");
            DomainValidation<bool>.Ensure(restaurant.IsOpenFor(startsAt, policy.DurationMinutes)).IsTrue("Sorry, the restaurant is closed at that time. Please choose another time.");
        }

        // Best fit: the smallest active table that seats the party and is free for the whole slot.
        public static Table PickTable(
            IEnumerable<Table> tables,
            IEnumerable<Reservation> existing,
            int guests,
            DateTime startsAt,
            int durationMinutes)
        {
            var blocking = existing.Where(r => IsBlocking(r.Status)).ToList();

            return tables
                .Where(t => t.IsActive && t.Capacity >= guests)
                .Where(t => !blocking.Any(r => r.TableId == t.Id && Overlaps(r.StartsAt, r.DurationMinutes, startsAt, durationMinutes)))
                .OrderBy(t => t.Capacity)
                .ThenBy(t => t.Label, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }
    }
}
