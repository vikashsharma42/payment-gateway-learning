using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services
{
    public sealed class PlatformMonitorService : IPlatformMonitorService
    {
        // Most recent rows shown on the SuperAdmin pages
        private const int MaxRows = 50;

        private readonly ApplicationDbContext _db;

        public PlatformMonitorService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PlatformStats> GetStatsAsync(DateTime day, CancellationToken ct = default)
        {
            var dayStart = day.Date;
            var nextDay = dayStart.AddDays(1);

            var restaurants = await _db.Restaurants.CountAsync(ct);
            var activeRestaurants = await _db.Restaurants.CountAsync(r => r.Status == RestaurantStatus.Active, ct);
            var users = await _db.Users.CountAsync(ct);
            var customers = await _db.Users.CountAsync(u => u.Role == UserRole.Customer, ct);
            var bookingsToday = await _db.Reservations.CountAsync(r => r.StartsAt >= dayStart && r.StartsAt < nextDay, ct);
            var placedOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Placed, ct);

            return new PlatformStats(restaurants, activeRestaurants, users, customers, bookingsToday, placedOrders);
        }

        public async Task<IReadOnlyList<Reservation>> GetRecentReservationsAsync(CancellationToken ct = default)
        {
            return await _db.Reservations.AsNoTracking()
                .Include(r => r.Restaurant)
                .OrderByDescending(r => r.StartsAt)
                .Take(MaxRows)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Orders>> GetRecentOrdersAsync(CancellationToken ct = default)
        {
            return await _db.Orders.AsNoTracking()
                .Include(o => o.Restaurant)
                .Include(o => o.Items)
                .Include(o => o.Payments)
                .OrderByDescending(o => o.CreatedDateTime)
                .Take(MaxRows)
                .AsSplitQuery()
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Payment>> GetRecentPaymentsAsync(CancellationToken ct = default)
        {
            return await _db.Payments.AsNoTracking()
                .Include(p => p.Order)
                .ThenInclude(o => o.Restaurant)
                .OrderByDescending(p => p.CreatedDateTime)
                .Take(MaxRows)
                .ToListAsync(ct);
        }
    }
}
