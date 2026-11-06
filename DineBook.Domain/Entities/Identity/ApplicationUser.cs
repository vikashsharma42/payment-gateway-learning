using System;
using DineBook.Domain.Abstractions;
using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;

namespace DineBook.Domain.Entities.Identity
{
    /// <summary>
    /// A person who can sign in. The password is never stored: only the hash produced by the application layer.
    /// </summary>
    public class ApplicationUser : AggregateRoot, IAuditableEntity
    {
        public const int MaxEmailLength = 254;
        public const int MaxNameLength = 100;

        public ApplicationUser(string email, string passwordHash, string firstName, string lastName, UserRole role)
        {
            DomainValidation<string>.Ensure(email).IsNotNullOrWhiteSpace().HasMaxLength(MaxEmailLength)
                .IsEmail("A valid email is required.");
            DomainValidation<string>.Ensure(passwordHash).IsNotNullOrWhiteSpace();
            DomainValidation<string>.Ensure(firstName).IsNotNullOrWhiteSpace().HasMaxLength(MaxNameLength);
            DomainValidation<string>.Ensure(lastName).IsNotNullOrWhiteSpace().HasMaxLength(MaxNameLength);
            DomainValidation<UserRole>.Ensure(role).IsDefinedEnum();

            Email = NormalizeEmail(email);
            PasswordHash = passwordHash;
            FirstName = firstName.Trim();
            LastName = lastName.Trim();
            Role = role;
            IsActive = true;
        }

        public string Email { get; private set; }

        public string PasswordHash { get; private set; }

        public string FirstName { get; private set; }

        public string LastName { get; private set; }

        public UserRole Role { get; private set; }

        // Inactive users cannot sign in, and their refresh tokens stop working
        public bool IsActive { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();

        // Emails are stored trimmed and lower-case, so one account per address
        public static string NormalizeEmail(string email) =>
            (email ?? string.Empty).Trim().ToLowerInvariant();

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
