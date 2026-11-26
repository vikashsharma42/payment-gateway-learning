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

        // Roles an admin can assign. Customers sign up themselves.
        private static readonly UserRole[] StaffRoles = { UserRole.Staff, UserRole.Manager, UserRole.Admin };

        private readonly ApplicationDbContext _db;
        private readonly IPasswordHasher _hasher;

        public RegistrationService(ApplicationDbContext db, IPasswordHasher hasher)
        {
            _db = db;
            _hasher = hasher;
        }

        public async Task<ApplicationUser> RegisterCustomerAsync(CustomerRegistrationRequest request, CancellationToken ct = default)
        {
            // A customer registers with one active restaurant and can then book at any of its locations
            var restaurantOpen = await _db.Restaurants.AnyAsync(
                r => r.Id == request.RestaurantId && r.Status == RestaurantStatus.Active, ct);
            if (!restaurantOpen)
                throw new DomainException("Choose a restaurant to register with.");

            var email = ApplicationUser.NormalizeEmail(request.Email);
            await EnsureEmailFreeAsync(email, ct);

            // Validates and normalizes the phone (digits only)
            var phone = ApplicationUser.NormalizePhone(request.Phone);

            if (await _db.Users.AnyAsync(u => u.Phone == phone, ct))
                throw new DomainException("An account already exists for this phone number.");

            var user = new ApplicationUser(
                email,
                _hasher.Hash(request.Password),
                request.FirstName,
                request.LastName,
                UserRole.Customer,
                phone,
                request.RestaurantId);

            _db.Users.Add(user);
            await SaveAsync(ct);

            return user;
        }

        public async Task<ApplicationUser> CreateStaffUserAsync(StaffUserRequest request, CancellationToken ct = default)
        {
            if (!StaffRoles.Contains(request.Role))
                throw new DomainException("Choose Staff, Manager or Admin.");

            var email = ApplicationUser.NormalizeEmail(request.Email);
            await EnsureEmailFreeAsync(email, ct);

            // Staff and managers must work at a restaurant. Admins are not tied to one.
            var restaurantId = request.Role == UserRole.Admin ? (long?)null : request.RestaurantId;
            if (restaurantId is null && request.Role != UserRole.Admin)
                throw new DomainException("Choose the restaurant for this staff member.");

            if (restaurantId is not null && !await _db.Restaurants.AnyAsync(r => r.Id == restaurantId.Value, ct))
                throw new DomainException("Restaurant not found.");

            // Staff phone is optional, so it is not asked for here
            var user = new ApplicationUser(
                email,
                _hasher.Hash(request.Password),
                request.FirstName,
                request.LastName,
                request.Role,
                phone: null,
                restaurantId);

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
