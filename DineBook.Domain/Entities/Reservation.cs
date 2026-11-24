using DineBook.Domain.Abstractions;
using DineBook.Domain.Enums;
using DineBook.Domain.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DineBook.Domain.Entities
{
    // One table booking, identified by a short code. It holds a table for its duration and collects the food ordered for it.
    public class Reservation : Entity, IAuditableEntity
    {
        public const int CodeLength = 8;
        public const int MinDurationMinutes = 30;
        public const int MaxSpecialRequestsLength = 200;

        private const int MinPhoneDigits = 10;
        private const int MaxPhoneDigits = 15;

        public Reservation(
            string code,
            long applicationUserId,
            long restaurantId,
            long tableId,
            string customerName,
            string customerPhone,
            string customerEmail,
            int guests,
            DateTime startsAt,
            int durationMinutes,
            string specialRequests)
        {
            DomainValidation<string>.Ensure(code).HasLength(CodeLength, "Booking code must be 8 characters.");
            DomainValidation<long>.Ensure(applicationUserId).IsGreaterThan(0, "A signed-in customer is required to book.");
            DomainValidation<string>.Ensure(customerName).IsNotNullOrWhiteSpace("Name is required.");
            DomainValidation<string>.Ensure(customerPhone)
                .IsPhoneNumber(MinPhoneDigits, MaxPhoneDigits, "Enter a valid phone number.");
            DomainValidation<string>.Ensure(customerEmail).IsEmail("A valid email is required.");
            DomainValidation<int>.Ensure(guests).IsGreaterThanOrEqualTo(1, "At least one guest is required.");
            DomainValidation<int>.Ensure(durationMinutes)
                .IsGreaterThanOrEqualTo(MinDurationMinutes, "A booking must last at least 30 minutes.");
            DomainValidation<string>.Ensure(specialRequests ?? string.Empty, nameof(specialRequests))
                .HasMaxLength(MaxSpecialRequestsLength, "Special requests can be at most 200 characters.");

            Code = code;
            ApplicationUserId = applicationUserId;
            RestaurantId = restaurantId;
            TableId = tableId;
            CustomerName = customerName.Trim();
            CustomerPhone = NormalizePhone(customerPhone);
            CustomerEmail = customerEmail.Trim().ToLowerInvariant();
            Guests = guests;
            StartsAt = startsAt;
            DurationMinutes = durationMinutes;
            SpecialRequests = (specialRequests ?? string.Empty).Trim();
            Status = ReservationStatus.Confirmed;
        }

        // Booking code the customer uses to view or cancel the booking
        public string Code { get; private set; }

        public long RestaurantId { get; private set; }

        // The signed-in customer who made the booking
        public long ApplicationUserId { get; private set; }

        public long TableId { get; private set; }

        public string CustomerName { get; private set; }

        // Stored as digits only
        public string CustomerPhone { get; private set; }

        public string CustomerEmail { get; private set; }

        public int Guests { get; private set; }

        // Restaurant local time
        public DateTime StartsAt { get; private set; }

        public int DurationMinutes { get; private set; }

        public string SpecialRequests { get; private set; }

        public ReservationStatus Status { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        public DateTime EndsAt => StartsAt.AddMinutes(DurationMinutes);

        public virtual Restaurant Restaurant { get; private set; }

        public virtual RestaurantTable Table { get; private set; }

        private readonly List<Orders> _foodOrders = [];
        public IReadOnlyCollection<Orders> FoodOrders => _foodOrders.AsReadOnly();

        public static string NormalizePhone(string phone) =>
            new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());

        public void Seat() => MoveFrom(ReservationStatus.Confirmed, ReservationStatus.Seated, "Only confirmed reservations can be seated.");

        public void Complete() => MoveFrom(ReservationStatus.Seated, ReservationStatus.Completed, "Only seated reservations can be completed.");

        public void MarkNoShow() => MoveFrom(ReservationStatus.Confirmed, ReservationStatus.NoShow, "Only confirmed reservations can be marked as no-show.");

        public void Cancel()
        {
            MoveFrom(ReservationStatus.Confirmed, ReservationStatus.Cancelled, "Only confirmed reservations can be cancelled.");
        }

        public bool IsBookedBy(long userId) => ApplicationUserId == userId;

        // Food can be ordered while the booking is still active (confirmed or seated, before it ends)
        public bool AcceptsFoodOrders(DateTime nowLocal) =>
            (Status == ReservationStatus.Confirmed || Status == ReservationStatus.Seated) && nowLocal < EndsAt;

        // The booking decides whether it still accepts food, so no caller can order against a cancelled or finished booking.
        public void AddFoodOrder(Orders order, DateTime nowLocal)
        {
            DomainValidation<Orders>.Ensure(order, "Order").IsNotNull();

            DomainValidation<bool>.Ensure(AcceptsFoodOrders(nowLocal))
                .IsTrue("Food can only be ordered while your booking is active.");
            DomainValidation<long>.Ensure(order.RestaurantId, nameof(order)).IsEqualTo(RestaurantId);

            order.AssignReservation(Id);
            _foodOrders.Add(order);
        }

        private void MoveFrom(ReservationStatus expected, ReservationStatus next, string message)
        {
            DomainValidation<ReservationStatus>.Ensure(Status).IsEqualTo(expected, message);

            Status = next;
        }
    }
}
