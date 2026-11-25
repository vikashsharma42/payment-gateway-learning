using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Exceptions;
using DineBook.Domain.ValueObjects;
using DineBook.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services
{
    public sealed class RestaurantAdminService : IRestaurantAdminService
    {
        private readonly ApplicationDbContext _db;

        public RestaurantAdminService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Restaurant> CreateRestaurantAsync(CreateRestaurantRequest request, CancellationToken ct = default)
        {
            var name = (request.Name ?? string.Empty).Trim();

            if (await _db.Restaurants.AnyAsync(r => r.Name == name, ct))
                throw new DomainException("A restaurant with this name already exists.");

            // Address and time rules are checked by the domain objects below
            var restaurant = new Restaurant(name, request.OpensAt, request.ClosesAt);
            if (await _db.Restaurants.AnyAsync(r => r.Slug == restaurant.Slug, ct))
            {
                throw new DomainException("A restaurant with this URL slug already exists. " +
                    "The current Domain model does not support assigning a unique slug.");
            }

            var address = CreateAddress(request.Street, request.City, request.State, request.Country, request.ZipCode);
            restaurant.AddLocation(new Location(request.LocationName, address, request.Phone));

            _db.Restaurants.Add(restaurant);
            await _db.SaveChangesAsync(ct);

            return restaurant;
        }

        public async Task<Location> AddLocationAsync(long restaurantId, AddLocationRequest request, CancellationToken ct = default)
        {
            var restaurant = await LoadRestaurantAsync(restaurantId, ct);

            var location = new Location(request.Name, CreateAddress(request.Street, request.City, request.State, request.Country, request.ZipCode), request.Phone);
            restaurant.AddLocation(location);
            await _db.SaveChangesAsync(ct);

            return location;
        }

        public async Task SetLocationActiveAsync(long restaurantId, long locationId, bool active, CancellationToken ct = default)
        {
            var restaurant = await LoadRestaurantAsync(restaurantId, ct);

            var location = restaurant.Locations
              .FirstOrDefault(l => l.Id == locationId)
              ?? throw new DomainException("Location not found.");

            if (location.IsActive == active)
                return;

            //TODO: The domain model currently does not support changing the status of a location. This is a limitation that needs to be addressed in the future.
            throw new DomainException("Changing location status is not supported by the current Domain model.");
        }

        public async Task UpdateChargesAsync(long restaurantId, decimal taxPercent, decimal systemAccessFee, CancellationToken ct = default)
        {
            var restaurant = await _db.Restaurants.FirstOrDefaultAsync(r => r.Id == restaurantId, ct)
                ?? throw new DomainException("Restaurant not found.");

            restaurant.UpdateCharges(taxPercent, systemAccessFee);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<Table> AddTableAsync(long restaurantId, long locationId, string label, int capacity, CancellationToken ct = default)
        {
            var restaurant = await LoadRestaurantAsync(restaurantId, ct);

            // The domain rejects duplicate labels, bad capacities and closed locations
            var table = new Table(label, capacity, string.Empty);
            restaurant.AddTable(locationId, table);
            await _db.SaveChangesAsync(ct);

            return table;
        }

        public async Task RenameTableAsync(long restaurantId, long tableId, string label, CancellationToken ct = default)
        {
            var table = await _db.Tables
                .FirstOrDefaultAsync(t => t.Id == tableId && t.RestaurantId == restaurantId, ct)
                ?? throw new DomainException("Table not found.");

            table.Rename(label);

            await _db.SaveChangesAsync(ct);
        }

        public async Task RemoveTableAsync(long restaurantId, long tableId, CancellationToken ct = default)
        {
            var table = await _db.Tables
                .FirstOrDefaultAsync(t => t.Id == tableId && t.RestaurantId == restaurantId, ct)
                ?? throw new DomainException("Table not found.");

            table.Delete();
            await _db.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<MenuItem>> GetMenuAsync(long restaurantId, CancellationToken ct = default)
        {
            return await _db.MenuItems.AsNoTracking()
                .Where(m => m.RestaurantId == restaurantId)
                .OrderBy(m => m.Category)
                .ThenBy(m => m.Name)
                .ToListAsync(ct);
        }

        public async Task<MenuItem> AddMenuItemAsync(
            long restaurantId, string name, string description, string category, decimal price,
            CancellationToken ct = default)
        {
            var restaurant = await _db.Restaurants.Include(r => r.MenuCategories).FirstOrDefaultAsync(r => r.Id == restaurantId, ct)
                ?? throw new DomainException("Restaurant not found.");

            var categoryName = (category ?? string.Empty).Trim();

            var menuCategory = restaurant.MenuCategories
               .FirstOrDefault(c => c.Name == categoryName);

            if (menuCategory is null)
            {
                menuCategory = new MenuCategory(categoryName);
                restaurant.AddMenuCategory(menuCategory);

                await _db.SaveChangesAsync(ct);
            }

            var item = new MenuItem(name, description, price);
            restaurant.AddMenuItem(menuCategory.Id, item);
            await _db.SaveChangesAsync(ct);

            return item;
        }

        public async Task SetMenuItemAvailabilityAsync(long menuItemId, bool available, CancellationToken ct = default)
        {
            var item = await FindMenuItemAsync(menuItemId, ct);

            item.SetAvailability(available);
            await _db.SaveChangesAsync(ct);
        }

        public async Task RemoveMenuItemAsync(long menuItemId, CancellationToken ct = default)
        {
            var item = await FindMenuItemAsync(menuItemId, ct);

            item.SetAvailability(false);
            await _db.SaveChangesAsync(ct);
        }


        private static Address CreateAddress(string street, string city, string state, string country, string zipCode) =>
            Address.Create(street, city, state, country, zipCode);

        private async Task<Restaurant> LoadRestaurantAsync(long restaurantId, CancellationToken ct) =>
            await _db.Restaurants.Include(r => r.Locations).Include(r => r.Tables)
                .FirstOrDefaultAsync(r => r.Id == restaurantId, ct)
            ?? throw new DomainException("Restaurant not found.");

        private async Task<MenuItem> FindMenuItemAsync(long menuItemId, CancellationToken ct) =>
            await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == menuItemId, ct)
            ?? throw new DomainException("Menu item not found.");
    }
}
