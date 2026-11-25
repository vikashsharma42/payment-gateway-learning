using DineBook.Application.Services.Interface;
using DineBook.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
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
                .AsSplitQuery()
                .FirstOrDefaultAsync(r => r.Id == restaurantId.Value, ct);

            if (restaurant is null)
                return null;

            var activeLocations = restaurant.Locations
                .Where(location => location.IsActive)
                .ToList();

            if (activeLocations.Count == 0)
                return null;

            var currentLocation = activeLocations.FirstOrDefault(location => locationId.HasValue && location.Id == locationId.Value) ?? activeLocations[0];

            return new StaffWorkspace(restaurant, activeLocations, currentLocation);
        }
    }
}
