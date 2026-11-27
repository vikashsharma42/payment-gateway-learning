using DineBook.Application.Paging;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
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
    public sealed class StaffWorkService : IStaffWorkService
    {
        // Most recent rows shown on each staff page
        private const int MaxRows = 200;

        private readonly ApplicationDbContext _db;

        public StaffWorkService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<Reservation>> GetReservationsAsync(long restaurantId, long locationId, DateTime from, CancellationToken ct = default)
        {
            return await _db.Reservations.AsNoTracking()
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .Where(r => r.RestaurantId == restaurantId && r.Table.LocationId == locationId && r.StartsAt >= from)
                .OrderBy(r => r.StartsAt)
                .Take(MaxRows)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Orders>> GetOrdersAsync(long restaurantId, long locationId, CancellationToken ct = default)
        {
            return await _db.Orders.AsNoTracking()
                .Include(o => o.Reservation)
                .ThenInclude(r => r.Table)
                .Include(o => o.Items)
                .Where(o => o.RestaurantId == restaurantId && o.Reservation.Table.LocationId == locationId)
                .OrderByDescending(o => o.CreatedDateTime)
                .Take(MaxRows)
                .AsSplitQuery()
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Reservation>> GetRestaurantReservationsAsync(long restaurantId, DateTime from, CancellationToken ct = default)
        {
            return await _db.Reservations.AsNoTracking()
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .Where(r => r.RestaurantId == restaurantId && r.StartsAt >= from)
                .OrderBy(r => r.StartsAt)
                .Take(MaxRows)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Orders>> GetRestaurantOrdersAsync(long restaurantId, CancellationToken ct = default)
        {
            return await _db.Orders.AsNoTracking()
                .Include(o => o.Reservation)
                .ThenInclude(r => r.Table)
                .Include(o => o.Items)
                .Where(o => o.RestaurantId == restaurantId)
                .OrderByDescending(o => o.CreatedDateTime)
                .Take(MaxRows)
                .AsSplitQuery()
                .ToListAsync(ct);
        }

        public Task<PagedResult<Reservation>> GetReservationsPageAsync(long restaurantId, long locationId, DateTime from, int page, int pageSize, CancellationToken ct = default) =>
            _db.Reservations.AsNoTracking()
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .Where(r => r.RestaurantId == restaurantId && r.Table.LocationId == locationId && r.StartsAt >= from)
                .OrderBy(r => r.StartsAt).ThenBy(r => r.Id)
                .ToPagedAsync(page, pageSize, ct);

        public Task<PagedResult<Orders>> GetOrdersPageAsync(long restaurantId, long locationId, int page, int pageSize, CancellationToken ct = default) =>
            _db.Orders.AsNoTracking()
                .Include(o => o.Reservation)
                .ThenInclude(r => r.Table)
                .Include(o => o.Items)
                .Where(o => o.RestaurantId == restaurantId && o.Reservation.Table.LocationId == locationId)
                .OrderByDescending(o => o.CreatedDateTime).ThenByDescending(o => o.Id)
                .AsSplitQuery()
                .ToPagedAsync(page, pageSize, ct);

        public Task<PagedResult<Reservation>> GetRestaurantReservationsPageAsync(long restaurantId, DateTime from, int page, int pageSize, CancellationToken ct = default) =>
            _db.Reservations.AsNoTracking()
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .Where(r => r.RestaurantId == restaurantId && r.StartsAt >= from)
                .OrderBy(r => r.StartsAt).ThenBy(r => r.Id)
                .ToPagedAsync(page, pageSize, ct);

        public Task<PagedResult<Orders>> GetRestaurantOrdersPageAsync(long restaurantId, int page, int pageSize, CancellationToken ct = default) =>
            _db.Orders.AsNoTracking()
                .Include(o => o.Reservation)
                .ThenInclude(r => r.Table)
                .Include(o => o.Items)
                .Where(o => o.RestaurantId == restaurantId)
                .OrderByDescending(o => o.CreatedDateTime).ThenByDescending(o => o.Id)
                .AsSplitQuery()
                .ToPagedAsync(page, pageSize, ct);

        public async Task EnsureReservationAtLocationAsync(long restaurantId, long locationId, long reservationId, CancellationToken ct = default)
        {
            var found = await _db.Reservations.AnyAsync(
                r => r.Id == reservationId && r.RestaurantId == restaurantId && r.Table.LocationId == locationId, ct);

            if (!found)
                throw new DomainException("That booking is not at this location.");
        }

        public async Task EnsureOrderAtLocationAsync(long restaurantId, long locationId, long orderId, CancellationToken ct = default)
        {
            var found = await _db.Orders.AnyAsync(
                o => o.Id == orderId && o.RestaurantId == restaurantId && o.Reservation.Table.LocationId == locationId, ct);

            if (!found)
                throw new DomainException("That order is not at this location.");
        }
    }
}
