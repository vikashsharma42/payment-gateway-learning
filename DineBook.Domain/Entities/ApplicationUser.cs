using DineBook.Domain.Abstractions;
using DineBook.Domain.Enums;
using DineBook.Domain.Validation;
using System;
using System.Linq;

namespace DineBook.Domain.Entities
{
    // A person who can sign in. The password is never stored: only the hash produced by the application layer.
    public class ApplicationUser : Entity, IAuditableEntity
    {
        public ApplicationUser(
            string email,
            string passwordHash,
            string firstName,
            string lastName,
            UserRole role,
            string phone,
            long? restaurantId)
        {
            DomainValidation<string>.Ensure(email).IsNotNullOrWhiteSpace().HasMaxLength(325).IsEmail("A valid email is required.");
            DomainValidation<string>.Ensure(passwordHash).IsNotNullOrWhiteSpace();
            DomainValidation<string>.Ensure(firstName).IsNotNullOrWhiteSpace().HasMaxLength(100);
            DomainValidation<string>.Ensure(lastName).IsNotNullOrWhiteSpace().HasMaxLength(100);
            DomainValidation<UserRole>.Ensure(role).IsDefinedEnum();

            var normalizedPhone = NormalizePhone(phone);
            if (role == UserRole.Customer)
                DomainValidation<string>.Ensure(normalizedPhone, nameof(phone)).IsNotNullOrWhiteSpace("A phone number is required for customers.").HasMaxLength(15);
            else
                DomainValidation<string>.Ensure(normalizedPhone, nameof(phone)).HasMaxLength(15);

            if (role == UserRole.Admin)
                DomainValidation<bool>.Ensure(restaurantId is null).IsTrue("Admins are not tied to a restaurant.");
            else
                DomainValidation<bool>.Ensure(restaurantId is not null && restaurantId > 0).IsTrue("Choose the restaurant for this user.");

            Email = NormalizeEmail(email);
            PasswordHash = passwordHash.Trim();
            FirstName = firstName.Trim();
            LastName = lastName.Trim();
            Role = role;
            // Null for staff without a phone, so several accounts can leave it empty without clashing on a unique index
            Phone = normalizedPhone.Length == 0 ? null : normalizedPhone;
            RestaurantId = role == UserRole.Admin ? null : restaurantId;
            IsActive = true;
        }

        // Required for Customer, Staff and Manager. Null only for Admin.
        public long? RestaurantId { get; private set; }

        public string Email { get; private set; }

        public string FirstName { get; private set; }

        public string LastName { get; private set; }

        public UserRole Role { get; private set; }

        public string PasswordHash { get; private set; }

        // Required for customers. Optional for staff.
        public string Phone { get; private set; }

        public bool IsActive { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();

        public void ChangePasswordHash(string passwordHash)
        {
            DomainValidation<string>.Ensure(passwordHash).IsNotNullOrWhiteSpace();
            PasswordHash = passwordHash.Trim();
        }

        // Customers, staff and managers belong to one restaurant. SuperAdmins belong to none.
        public bool BelongsTo(long restaurantId) => RestaurantId == restaurantId;

        public void Activate() => IsActive = true;

        public void Deactivate() => IsActive = false;

        // Same rules as the constructor: customers must keep a phone number
        public void UpdateProfile(string firstName, string lastName, string phone)
        {
            DomainValidation<string>.Ensure(firstName).IsNotNullOrWhiteSpace().HasMaxLength(100);
            DomainValidation<string>.Ensure(lastName).IsNotNullOrWhiteSpace().HasMaxLength(100);

            var normalizedPhone = NormalizePhone(phone);
            if (Role == UserRole.Customer)
                DomainValidation<string>.Ensure(normalizedPhone, nameof(phone)).IsNotNullOrWhiteSpace("A phone number is required for customers.").HasMaxLength(15);
            else
                DomainValidation<string>.Ensure(normalizedPhone, nameof(phone)).HasMaxLength(15);

            FirstName = firstName.Trim();
            LastName = lastName.Trim();
            Phone = normalizedPhone.Length == 0 ? null : normalizedPhone;
        }

        public void ChangeEmail(string email)
        {
            DomainValidation<string>.Ensure(email).IsNotNullOrWhiteSpace().HasMaxLength(325).IsEmail("A valid email is required.");
            Email = NormalizeEmail(email);
        }

        // Stored trimmed and lower-case, so one account per address
        public static string NormalizeEmail(string email) =>
            (email ?? string.Empty).Trim().ToLowerInvariant();

        // Digits only, so the same phone is always stored and compared the same way
        public static string NormalizePhone(string phone) =>
            new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
    }
}
