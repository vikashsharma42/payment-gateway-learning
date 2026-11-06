using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities.Dining;
using DineBook.Domain.Entities.Menu;
using DineBook.Domain.Entities.Organization;
using DineBook.Domain.Exceptions;
using DineBook.Domain.ValueObjects;
using DineBook.Infrastructure.Database;

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
            var address = Address.Create(request.Street, request.City, request.State, request.Pincode);
            var restaurant = new Restaurant(name, address, request.OpensAt, request.ClosesAt);

            _db.Restaurants.Add(restaurant);
            await _db.SaveChangesAsync(ct);

            return restaurant;
        }

        public async Task<RestaurantTable> AddTableAsync(long restaurantId, string label, int capacity, CancellationToken ct = default)
        {
            var restaurant = await LoadRestaurantAsync(restaurantId, ct);

            // The domain rejects duplicate labels and bad capacities
            var table = new RestaurantTable(label, capacity, string.Empty);
            restaurant.AddTable(table);
            await _db.SaveChangesAsync(ct);

            return table;
        }

        public async Task RenameTableAsync(long restaurantId, long tableId, string label, CancellationToken ct = default)
        {
            var restaurant = await LoadRestaurantAsync(restaurantId, ct);

            restaurant.RenameTable(restaurant.FindTable(tableId), label);
            await _db.SaveChangesAsync(ct);
        }

        public async Task RemoveTableAsync(long restaurantId, long tableId, CancellationToken ct = default)
        {
            var restaurant = await LoadRestaurantAsync(restaurantId, ct);

            restaurant.RemoveTable(restaurant.FindTable(tableId));
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

            var item = new MenuItem(name, description, price);
            restaurant.AddMenuItem(item, restaurant.GetOrAddMenuCategory(category));
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

            item.Delete();
            await _db.SaveChangesAsync(ct);
        }

        private async Task<Restaurant> LoadRestaurantAsync(long restaurantId, CancellationToken ct) =>
            await _db.Restaurants.Include(r => r.Tables).FirstOrDefaultAsync(r => r.Id == restaurantId, ct)
            ?? throw new DomainException("Restaurant not found.");

        private async Task<MenuItem> FindMenuItemAsync(long menuItemId, CancellationToken ct) =>
            await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == menuItemId, ct)
            ?? throw new DomainException("Menu item not found.");
    }
}
