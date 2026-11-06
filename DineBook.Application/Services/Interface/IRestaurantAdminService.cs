using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DineBook.Domain.Entities.Dining;
using DineBook.Domain.Entities.Menu;
using DineBook.Domain.Entities.Organization;

namespace DineBook.Application.Services.Interface
{
    /// <summary>
    /// Admin use cases: create restaurants, manage tables and the menu. Callers must be authorized.
    /// </summary>
    public interface IRestaurantAdminService
    {
        Task<Restaurant> CreateRestaurantAsync(CreateRestaurantRequest request, CancellationToken ct = default);

        Task<RestaurantTable> AddTableAsync(long restaurantId, string label, int capacity, CancellationToken ct = default);

        Task RenameTableAsync(long restaurantId, long tableId, string label, CancellationToken ct = default);

        /// <summary>Soft-deletes a table. Past bookings keep it; new bookings no longer use it.</summary>
        Task RemoveTableAsync(long restaurantId, long tableId, CancellationToken ct = default);

        /// <summary>All active items for a restaurant, including unavailable ones.</summary>
        Task<IReadOnlyList<MenuItem>> GetMenuAsync(long restaurantId, CancellationToken ct = default);

        Task<MenuItem> AddMenuItemAsync(
            long restaurantId, string name, string description, string category, decimal price,
            CancellationToken ct = default);

        Task SetMenuItemAvailabilityAsync(long menuItemId, bool available, CancellationToken ct = default);

        /// <summary>Soft-deletes a menu item. Past orders keep it.</summary>
        Task RemoveMenuItemAsync(long menuItemId, CancellationToken ct = default);
    }

    public sealed record CreateRestaurantRequest(
        string Name,
        string Street,
        string City,
        string State,
        string Pincode,
        TimeOnly OpensAt,
        TimeOnly ClosesAt);
}
