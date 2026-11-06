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
            DomainValidation<bool>.Ensure(restaurant.IsActive)
                .IsTrue("This restaurant is not taking bookings right now.");
            DomainValidation<int>.Ensure(guests)
                .IsBetween(1, policy.MaxGuests, $"Party size must be between 1 and {policy.MaxGuests} guests.");
            DomainValidation<DateTime>.Ensure(startsAt)
                .IsGreaterThanOrEqualTo(nowLocal.AddMinutes(policy.MinNoticeMinutes), $"Please book at least {policy.MinNoticeMinutes} minutes in advance.");
            DomainValidation<DateTime>.Ensure(startsAt.Date, nameof(startsAt))
                .IsLessThanOrEqualTo(nowLocal.Date.AddDays(policy.MaxAdvanceDays), $"Bookings open up to {policy.MaxAdvanceDays} days in advance.");
            DomainValidation<bool>.Ensure(restaurant.IsOpenFor(startsAt, policy.DurationMinutes))
                .IsTrue("The restaurant is closed at that time.");
        }

        // Best fit: the smallest active table that seats the party and is free for the whole slot.
        public static RestaurantTable PickTable(
            IEnumerable<RestaurantTable> tables,
            IEnumerable<Reservation> existing,
            int guests,
            DateTime startsAt,
            int durationMinutes)
        {
            var blocking = existing.Where(r => IsBlocking(r.Status)).ToList();

            return tables
                .Where(t => t.IsActive && t.Capacity >= guests)
                .Where(t => !blocking.Any(r =>
                    r.TableId == t.Id && Overlaps(r.StartsAt, r.DurationMinutes, startsAt, durationMinutes)))
                .OrderBy(t => t.Capacity)
                .ThenBy(t => t.Label, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }
    }
}
