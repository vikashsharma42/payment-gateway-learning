using DineBook.Application.JWTSecurity;
using DineBook.Application.Security;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;
using DineBook.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services
{
    public sealed class RegistrationService : IRegistrationService
    {
        public const int MinPasswordLength = 12;
        public const int MaxPasswordLength = 128;

        // Roles an admin can assign. Customers sign up themselves.
        private static readonly UserRole[] StaffRoles = { UserRole.Staff, UserRole.Manager, UserRole.Admin };

        private readonly ApplicationDbContext _db;
        private readonly IPasswordHasher _hasher;
        private readonly IJwtSignInManager _signIn;

        public RegistrationService(ApplicationDbContext db, IPasswordHasher hasher, IJwtSignInManager signIn)
        {
            _db = db;
            _hasher = hasher;
            _signIn = signIn;
        }

        public async Task<AccountSession> RegisterCustomerAsync(CustomerRegistrationRequest request, CancellationToken ct = default)
        {
            var email = ApplicationUser.NormalizeEmail(request.Email);
            ValidatePassword(request.Password);
            await EnsureEmailFreeAsync(email, ct);

            // Validates and normalizes the phone (digits only)
            var phone = Customer.NormalizePhone(request.Phone);
            var fullName = $"{request.FirstName} {request.LastName}".Trim();

            // A guest who booked before may already exist with this phone
            var guest = await _db.Customers.FirstOrDefaultAsync(c => c.Phone == phone, ct);
            if (guest?.ApplicationUserId is not null)
                throw new DomainException("An account already exists for this phone number.");

            var user = new ApplicationUser(email, _hasher.Hash(request.Password), request.FirstName, request.LastName, UserRole.Customer);
            _db.Users.Add(user);
            await SaveAsync(ct);

            if (guest is null)
            {
                guest = new Customer(fullName, phone, email);
                _db.Customers.Add(guest);
            }
            else
            {
                guest.UpdateContact(fullName, email);
            }

            guest.LinkUser(user.Id);
            await SaveAsync(ct);

            var tokens = await _signIn.SignInExternalAsync(user);
            return new AccountSession(tokens, user.Role);
        }

        public async Task<ApplicationUser> CreateStaffUserAsync(StaffUserRequest request, CancellationToken ct = default)
        {
            if (!StaffRoles.Contains(request.Role))
                throw new DomainException("Choose Staff, Manager or Admin.");

            var email = ApplicationUser.NormalizeEmail(request.Email);
            ValidatePassword(request.Password);
            await EnsureEmailFreeAsync(email, ct);

            var user = new ApplicationUser(email, _hasher.Hash(request.Password), request.FirstName, request.LastName, request.Role);
            _db.Users.Add(user);
            await SaveAsync(ct);

            return user;
        }

        public async Task<IReadOnlyList<ApplicationUser>> GetUsersAsync(CancellationToken ct = default)
        {
            return await _db.Users.AsNoTracking()
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FirstName)
                .ToListAsync(ct);
        }

        public async Task<bool> IsEmailAvailableAsync(string email, CancellationToken ct = default)
        {
            var normalized = ApplicationUser.NormalizeEmail(email);
            if (normalized.Length == 0)
                return true; // nothing to check yet; the form's required rule handles empty input

            return !await _db.Users.AnyAsync(u => u.Email == normalized, ct);
        }

        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new DomainException("Password is required.");

            if (password.Length < MinPasswordLength)
                throw new DomainException($"Password must be at least {MinPasswordLength} characters.");

            if (password.Length > MaxPasswordLength)
                throw new DomainException($"Password must be at most {MaxPasswordLength} characters.");
        }

        private async Task EnsureEmailFreeAsync(string email, CancellationToken ct)
        {
            if (await _db.Users.AnyAsync(u => u.Email == email, ct))
                throw new DomainException("An account with this email already exists.");
        }

        // A concurrent sign-up can take the same email or phone between the check and the save
        private async Task SaveAsync(CancellationToken ct)
        {
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                throw new DomainException("That email or phone number is already registered.");
            }
        }
    }
}
