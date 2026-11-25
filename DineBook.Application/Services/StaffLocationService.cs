using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services
{
    public sealed class StaffLocationService : IStaffLocationService
    {
        private readonly ApplicationDbContext _db;

        public StaffLocationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<StaffWorkspace> GetWorkspaceAsync(long userId, long? locationId, CancellationToken ct = default)
        {
            var restaurantId = await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.RestaurantId)
                .FirstOrDefaultAsync(ct);

            if (restaurantId is null)
                return null;

            var restaurant = await _db.Restaurants.AsNoTracking()
                .Include(r => r.Locations)
                .Include(r => r.Tables)
                .FirstOrDefaultAsync(r => r.Id == restaurantId.Value, ct);

            if (restaurant is null)
                return null;

            var open = restaurant.ActiveLocations.ToList();
            if (open.Count == 0)
                return null;

            // Only a location of this restaurant that is open can be the current one
            var current = open.FirstOrDefault(l => locationId.HasValue && l.Id == locationId.Value) ?? open[0];

            return new StaffWorkspace(restaurant, open, current);
        }
    }
}
