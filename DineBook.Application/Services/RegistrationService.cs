using DineBook.Application.Paging;
using DineBook.Application.Security;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;
using DineBook.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using System;
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
            await EnsureEmailFreeAsync(email, request.RestaurantId, ct);

            // Stored form: dial code + national number, digits only. The web layer resolves what the guest typed.
            var phone = ApplicationUser.NormalizePhone(request.Phone);

            // Phone is unique per restaurant, so the same number can register at another restaurant
            if (await _db.Users.AnyAsync(u => u.RestaurantId == request.RestaurantId && u.Phone == phone, ct))
                throw new DomainException("An account already exists for this phone number at this restaurant.");

            var user = new ApplicationUser(
                email,
                _hasher.Hash(request.Password),
                request.FirstName,
                request.LastName,
                UserRole.Customer,
                request.CountryCode,
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

            // Staff and managers must work at a restaurant. Admins are not tied to one.
            var restaurantId = request.Role == UserRole.Admin ? (long?)null : request.RestaurantId;
            if (restaurantId is null && request.Role != UserRole.Admin)
                throw new DomainException("Choose the restaurant for this staff member.");

            if (restaurantId is not null && !await _db.Restaurants.AnyAsync(r => r.Id == restaurantId.Value, ct))
                throw new DomainException("Restaurant not found.");

            await EnsureEmailFreeAsync(email, restaurantId, ct);

            // Staff phone is optional, so it is not asked for here
            var user = new ApplicationUser(
                email,
                _hasher.Hash(request.Password),
                request.FirstName,
                request.LastName,
                request.Role,
                countryCode: DineBook.Domain.Enums.CountryRules.Default,
                phone: null,
                restaurantId);

            _db.Users.Add(user);
            await SaveAsync(ct);

            return user;
        }

        public Task<PagedResult<ApplicationUser>> GetUsersAsync(string search, int page, int pageSize, CancellationToken ct = default)
        {
            var term = (search ?? string.Empty).Trim();

            var query = _db.Users.AsNoTracking().AsQueryable();
            if (term.Length > 0)
            {
                query = query.Where(u =>
                    u.Email.Contains(term) ||
                    u.FirstName.Contains(term) ||
                    u.LastName.Contains(term) ||
                    (u.Phone != null && u.Phone.Contains(term)));
            }

            // Stable order so that paging never repeats or skips a user
            return query
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Id)
                .ToPagedAsync(page, pageSize, ct);
        }

        public async Task<bool> IsEmailAvailableAsync(string email, long? restaurantId, CancellationToken ct = default)
        {
            var normalized = ApplicationUser.NormalizeEmail(email);
            if (normalized.Length == 0)
                return true; // nothing to check yet; the form's required rule handles empty input

            return !await EmailTakenAsync(normalized, restaurantId, ct);
        }

        private async Task EnsureEmailFreeAsync(string email, long? restaurantId, CancellationToken ct)
        {
            if (await EmailTakenAsync(email, restaurantId, ct))
                throw new DomainException("An account with this email already exists at this restaurant.");
        }

        // Email is unique per restaurant. Admins (no restaurant) are checked against other admins only.
        private Task<bool> EmailTakenAsync(string email, long? restaurantId, CancellationToken ct) =>
            restaurantId is null
                ? _db.Users.AnyAsync(u => u.Email == email && u.RestaurantId == null, ct)
                : _db.Users.AnyAsync(u => u.Email == email && u.RestaurantId == restaurantId.Value, ct);

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
