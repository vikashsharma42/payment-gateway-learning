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
        public const int MaxEmailLength = 254;
        public const int MaxNameLength = 100;

        public ApplicationUser(string email, string passwordHash, string firstName, string lastName, UserRole role, string phone)
        {
            DomainValidation<string>.Ensure(email).IsNotNullOrWhiteSpace().HasMaxLength(MaxEmailLength).IsEmail("A valid email is required.");
            DomainValidation<string>.Ensure(passwordHash).IsNotNullOrWhiteSpace();
            DomainValidation<string>.Ensure(firstName).IsNotNullOrWhiteSpace().HasMaxLength(MaxNameLength);
            DomainValidation<string>.Ensure(lastName).IsNotNullOrWhiteSpace().HasMaxLength(MaxNameLength);
            DomainValidation<UserRole>.Ensure(role).IsDefinedEnum();
            DomainValidation<string>.Ensure(phone).IsNotNullOrWhiteSpace().HasMaxLength(15);

            Email = NormalizeEmail(email);
            PasswordHash = passwordHash;
            FirstName = firstName.Trim();
            LastName = lastName.Trim();
            Role = role;
            Phone = NormalizePhone(phone);
            IsActive = true;
        }

        //null for only admin users, otherwise the restaurant they work at. Customers must have register to a restaurant.
        public long? RestaurantId { get; private set; }

        public string Email { get; private set; }

        public string FirstName { get; private set; }

        public string LastName { get; private set; }

        public UserRole Role { get; private set; }

        public string PasswordHash { get; private set; }

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

        public void Activate() => IsActive = true;

        public void Deactivate() => IsActive = false;


        // Stored trimmed and lower-case, so one account per address
        public static string NormalizeEmail(string email) =>
            (email ?? string.Empty).Trim().ToLowerInvariant();

        public static string NormalizePhone(string phone) =>
            new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
    }
}
