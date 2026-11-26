using DineBook.Domain.Abstractions;
using DineBook.Domain.Enums;
using DineBook.Domain.Validation;
using System;
using System.Collections.Generic;

namespace DineBook.Domain.Entities
{
    // One table booking, identified by a short code. It holds a table for its duration and collects the food ordered for it.
    // The guest's name, phone and email are read from the customer account, not copied here.
    public class Reservation : Entity, IAuditableEntity
    {
        // The customer must be a saved Customer account. Restaurant.AddReservation checks it is registered with that restaurant.
        public Reservation(
            string code,
            ApplicationUser customer,
            int guests,
            DateTime startsAt,
            int durationMinutes,
            string specialRequests)
        {
            DomainValidation<ApplicationUser>.Ensure(customer, nameof(customer)).IsNotNull();
            DomainValidation<bool>.Ensure(customer.Role == UserRole.Customer).IsTrue("Only customers can book a table.");
            DomainValidation<bool>.Ensure(customer.IsActive).IsTrue("This account is inactive.");
            DomainValidation<long>.Ensure(customer.Id, nameof(customer)).IsGreaterThan(0, "Save the customer before booking.");

            DomainValidation<string>.Ensure(code).HasLength(8, "Booking code must be 8 characters.");
            DomainValidation<int>.Ensure(guests).IsGreaterThanOrEqualTo(1, "At least one guest is required.");
            DomainValidation<int>.Ensure(durationMinutes).IsGreaterThanOrEqualTo(30, "A booking must last at least 30 minutes.");
            DomainValidation<string>.Ensure(specialRequests ?? string.Empty, nameof(specialRequests)).HasMaxLength(200, "Special requests can be at most 200 characters.");

            Code = code;
            Customer = customer;
            ApplicationUserId = customer.Id;
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

        // Guest details come from the customer account
        public string CustomerName => Customer?.FullName;

        public string CustomerPhone => Customer?.Phone;

        public string CustomerEmail => Customer?.Email;

        public virtual Restaurant Restaurant { get; private set; }

        public virtual ApplicationUser Customer { get; private set; }

        public virtual Table Table { get; private set; }

        private readonly List<Orders> _foodOrders = [];
        public IReadOnlyCollection<Orders> FoodOrders => _foodOrders.AsReadOnly();

        public void Seat() => MoveFrom(ReservationStatus.Confirmed, ReservationStatus.Seated, "Only confirmed reservations can be seated.");

        public void Complete() => MoveFrom(ReservationStatus.Seated, ReservationStatus.Completed, "Only seated reservations can be completed.");

        public void MarkNoShow() => MoveFrom(ReservationStatus.Confirmed, ReservationStatus.NoShow, "Only confirmed reservations can be marked as no-show.");

        public void Cancel() => MoveFrom(ReservationStatus.Confirmed, ReservationStatus.Cancelled, "Only confirmed reservations can be cancelled.");

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

        private void MoveFrom(ReservationStatus expected, ReservationStatus next, string message)
        {
            DomainValidation<ReservationStatus>.Ensure(Status).IsEqualTo(expected, message);
            Status = next;
        }
    }
}
