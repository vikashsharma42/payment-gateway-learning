using DineBook.Domain.Abstractions;
using DineBook.Domain.Enums;
using DineBook.Domain.Validation;
using System;
using System.Collections.Generic;

namespace DineBook.Domain.Entities
{
    // One table booking, identified by a short code. It holds a table for its duration and collects the food ordered for it.
    public class Reservation : Entity, IAuditableEntity
    {
        public const int CodeLength = 8;
        public const int MinDurationMinutes = 30;
        public const int MaxSpecialRequestsLength = 200;

        public Reservation(
            string code,
            int guests,
            DateTime startsAt,
            int durationMinutes,
            string specialRequests)
        {
            DomainValidation<string>.Ensure(code).HasLength(CodeLength, "Booking code must be 8 characters.");
            DomainValidation<int>.Ensure(guests).IsGreaterThanOrEqualTo(1, "At least one guest is required.");
            DomainValidation<int>.Ensure(durationMinutes).IsGreaterThanOrEqualTo(MinDurationMinutes, "A booking must last at least 30 minutes.");
            DomainValidation<string>.Ensure(specialRequests ?? string.Empty, nameof(specialRequests)).HasMaxLength(MaxSpecialRequestsLength, "Special requests can be at most 200 characters.");

            Code = code;
            Guests = guests;
            StartsAt = startsAt;
            DurationMinutes = durationMinutes;
            SpecialRequests = (specialRequests ?? string.Empty).Trim();
            Status = ReservationStatus.Confirmed;
        }

        // Booking code the customer uses to view or cancel the booking
        public string Code { get; private set; }

        public long RestaurantId { get; internal set; }

        public long ApplicationUserId { get; private set; }

        public long TableId { get; internal set; }

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

        public virtual ApplicationUser Customer { get; private set; }

        public virtual Table Table { get; private set; }

        private readonly List<Orders> _foodOrders = [];
        public IReadOnlyCollection<Orders> FoodOrders => _foodOrders.AsReadOnly();

        public void Seat() => MoveFrom(ReservationStatus.Confirmed, ReservationStatus.Seated, "Only confirmed reservations can be seated.");

        public void Complete() => MoveFrom(ReservationStatus.Seated, ReservationStatus.Completed, "Only seated reservations can be completed.");

        public void MarkNoShow() => MoveFrom(ReservationStatus.Confirmed, ReservationStatus.NoShow, "Only confirmed reservations can be marked as no-show.");

        // Food can be ordered while the booking is still active (confirmed or seated, before it ends)
        public bool AcceptsFoodOrders(DateTime nowLocal) =>
            (Status == ReservationStatus.Confirmed || Status == ReservationStatus.Seated) && nowLocal < EndsAt;

        // The booking decides whether it still accepts food, so no caller can order against a cancelled or finished booking.
        public void AddFoodOrder(Orders order, DateTime nowLocal)
        {
            DomainValidation<Orders>.Ensure(order, "Order").IsNotNull();
            DomainValidation<bool>.Ensure(AcceptsFoodOrders(nowLocal)).IsTrue("Food can only be ordered while your booking is active.");

            order.RestaurantId = RestaurantId;
            order.ReservationId = Id;
            _foodOrders.Add(order);
        }

        // Called by Customer.AddReservation. A new customer has Id 0 until saved; EF fixes the key from the navigation.
        internal void AssignCustomer(ApplicationUser customer)
        {
            Customer = customer;
            ApplicationUserId = customer.Id;
        }

        private void MoveFrom(ReservationStatus expected, ReservationStatus next, string message)
        {
            DomainValidation<ReservationStatus>.Ensure(Status).IsEqualTo(expected, message);
            Status = next;
        }
    }
}
