using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;
using DineBook.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services
{
    public sealed class CustomerService : ICustomerService
    {
        // Most recent rows shown on each customer page
        private const int MaxRows = 100;

        private readonly ApplicationDbContext _db;

        public CustomerService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ApplicationUser> FindProfileAsync(long customerId, CancellationToken ct = default)
        {
            return await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == customerId, ct);
        }

        public async Task<ApplicationUser> GetProfileAsync(long customerId, CancellationToken ct = default)
        {
            return await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == customerId && u.Role == UserRole.Customer, ct)
                ?? throw new DomainException("Account not found. Please sign in again.");
        }

        public async Task UpdateProfileAsync(long customerId, string firstName, string lastName, string phone, CancellationToken ct = default)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == customerId && u.Role == UserRole.Customer, ct)
                ?? throw new DomainException("Account not found. Please sign in again.");

            // The domain validates and normalizes the name and phone
            user.UpdateProfile(firstName, lastName, phone);

            // Phone is unique per restaurant, so another account at the same restaurant cannot use it
            var phoneTaken = await _db.Users.AnyAsync(
                u => u.Id != user.Id && u.RestaurantId == user.RestaurantId && u.Phone == user.Phone, ct);
            if (phoneTaken)
                throw new DomainException("An account already exists for this phone number at this restaurant.");

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                throw new DomainException("That phone number is already registered at this restaurant.");
            }
        }

        public async Task<IReadOnlyList<Reservation>> GetReservationsAsync(long customerId, CancellationToken ct = default)
        {
            return await _db.Reservations.AsNoTracking()
                .Include(r => r.Restaurant)
                .Where(r => r.ApplicationUserId == customerId)
                .OrderByDescending(r => r.StartsAt)
                .Take(MaxRows)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Orders>> GetOrdersAsync(long customerId, CancellationToken ct = default)
        {
            return await _db.Orders.AsNoTracking()
                .Include(o => o.Restaurant)
                .Include(o => o.Reservation)
                .Include(o => o.Items)
                .Where(o => o.Reservation.ApplicationUserId == customerId)
                .OrderByDescending(o => o.CreatedDateTime)
                .Take(MaxRows)
                .AsSplitQuery()
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Orders>> GetRestaurantOrdersAsync(long customerId, long restaurantId, CancellationToken ct = default)
        {
            return await _db.Orders.AsNoTracking()
                .Include(o => o.Restaurant)
                .Include(o => o.Reservation)
                .Include(o => o.Items)
                .Where(o => o.Reservation.ApplicationUserId == customerId && o.RestaurantId == restaurantId)
                .OrderByDescending(o => o.CreatedDateTime)
                .Take(MaxRows)
                .AsSplitQuery()
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Payment>> GetPaymentsAsync(long customerId, CancellationToken ct = default)
        {
            return await _db.Payments.AsNoTracking()
                .Include(p => p.Order)
                .ThenInclude(o => o.Restaurant)
                .Where(p => p.Order.Reservation.ApplicationUserId == customerId)
                .OrderByDescending(p => p.CreatedDateTime)
                .Take(MaxRows)
                .ToListAsync(ct);
        }
    }
}
