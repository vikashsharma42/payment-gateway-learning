using DineBook.Domain.Abstractions;
using DineBook.Domain.Enums;
using DineBook.Domain.Validation;
using System;
using System.Linq;

namespace DineBook.Domain.Entities
{
    // A person who can sign in. Every booking belongs to a customer account. The password is never stored: only the hash.
    public class ApplicationUser : Entity, IAuditableEntity
    {
        public const int MaxEmailLength = 254;
        public const int MaxNameLength = 100;
        public const int MinPhoneDigits = 10;
        public const int MaxPhoneDigits = 15;

        // phone is required for customers and optional for staff
        public ApplicationUser(string email, string passwordHash, string firstName, string lastName, UserRole role, string phone = "")
        {
            DomainValidation<string>.Ensure(email).IsNotNullOrWhiteSpace().HasMaxLength(MaxEmailLength)
                .IsEmail("A valid email is required.");
            DomainValidation<string>.Ensure(passwordHash).IsNotNullOrWhiteSpace();
            DomainValidation<string>.Ensure(firstName).IsNotNullOrWhiteSpace().HasMaxLength(MaxNameLength);
            DomainValidation<string>.Ensure(lastName).IsNotNullOrWhiteSpace().HasMaxLength(MaxNameLength);
            DomainValidation<UserRole>.Ensure(role).IsDefinedEnum();

            var digits = NormalizePhone(phone);
            if (role == UserRole.Customer)
                DomainValidation<string>.Ensure(digits).IsPhoneNumber(MinPhoneDigits, MaxPhoneDigits, "Enter a valid phone number.");

            Email = NormalizeEmail(email);
            PasswordHash = passwordHash;
            FirstName = firstName.Trim();
            LastName = lastName.Trim();
            Phone = digits;
            Role = role;
            IsActive = true;
        }

        public string Email { get; private set; }

        public string PasswordHash { get; private set; }

        public string FirstName { get; private set; }

        public string LastName { get; private set; }

        // Digits only. Empty for staff accounts without a phone number.
        public string Phone { get; private set; }

        public UserRole Role { get; private set; }

        // Inactive users cannot sign in
        public bool IsActive { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();

        // Stored trimmed and lower-case, so one account per address
        public static string NormalizeEmail(string email) =>
            (email ?? string.Empty).Trim().ToLowerInvariant();

        public static string NormalizePhone(string phone) =>
            new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());

        public void ChangePasswordHash(string passwordHash)
        {
            DomainValidation<string>.Ensure(passwordHash).IsNotNullOrWhiteSpace();

            PasswordHash = passwordHash;
        }

        public void ChangeRole(UserRole role)
        {
            DomainValidation<UserRole>.Ensure(role).IsDefinedEnum();

            Role = role;
        }

        public void Activate() => IsActive = true;

        public void Deactivate() => IsActive = false;
    }
}
