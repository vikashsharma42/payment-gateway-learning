using DineBook.Domain.Abstractions;
using DineBook.Domain.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DineBook.Domain.Entities
{
    // A guest who books tables. Identified by phone number, the same second factor bookings already use.
    // Reservations keep their own copy of the contact details, so later changes here do not rewrite history.
    public class Customer : Entity, IAuditableEntity
    {
        public const int MaxNameLength = 100;
        public const int MaxEmailLength = 254;
        public const int MinPhoneDigits = 10;
        public const int MaxPhoneDigits = 15;

        public Customer(string name, string phone, string email)
        {
            DomainValidation<string>.Ensure(name).IsNotNullOrWhiteSpace().HasMaxLength(MaxNameLength);
            DomainValidation<string>.Ensure(email).IsNotNullOrWhiteSpace().HasMaxLength(MaxEmailLength)
                .IsEmail("A valid email is required.");

            Name = name.Trim();
            Phone = NormalizePhone(phone);
            Email = email.Trim().ToLowerInvariant();
        }

        // Set when the customer also has a sign-in account
        public long? ApplicationUserId { get; private set; }

        public string Name { get; private set; }

        // Digits only
        public string Phone { get; private set; }

        public string Email { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        public virtual ApplicationUser ApplicationUser { get; set; }

        private readonly List<Reservation> _reservations = [];
        public IReadOnlyCollection<Reservation> Reservations => _reservations.AsReadOnly();

        public static string NormalizePhone(string phone)
        {
            DomainValidation<string>.Ensure(phone)
                .IsPhoneNumber(MinPhoneDigits, MaxPhoneDigits, "Enter a valid phone number.");

            return new string(phone.Where(char.IsDigit).ToArray());
        }

        public void UpdateContact(string name, string email)
        {
            DomainValidation<string>.Ensure(name).IsNotNullOrWhiteSpace().HasMaxLength(MaxNameLength);
            DomainValidation<string>.Ensure(email).IsNotNullOrWhiteSpace().HasMaxLength(MaxEmailLength)
                .IsEmail("A valid email is required.");

            Name = name.Trim();
            Email = email.Trim().ToLowerInvariant();
        }

        public void LinkUser(long applicationUserId)
        {
            DomainValidation<long>.Ensure(applicationUserId).IsGreaterThan(0, "User is required.");

            ApplicationUserId = applicationUserId;
        }

        public void AddReservation(Reservation reservation)
        {
            DomainValidation<Reservation>.Ensure(reservation).IsNotNull();

            reservation.AssignCustomer(this);
            _reservations.Add(reservation);
        }
    }
}
