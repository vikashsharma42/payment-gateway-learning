using DineBook.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services.Interface
{
    /// <summary>
    /// Admin use cases: create restaurants, manage locations, tables and the menu. Callers must be authorized.
    /// </summary>
    public interface IRestaurantAdminService
    {
        /// <summary>Creates the restaurant and its first location (the address is stored on the location).</summary>
        Task<Restaurant> CreateRestaurantAsync(CreateRestaurantRequest request, CancellationToken ct = default);

        Task<Location> AddLocationAsync(long restaurantId, AddLocationRequest request, CancellationToken ct = default);

        /// <summary>Closes or reopens a location for staff. The last open location cannot be closed.</summary>
        Task SetLocationActiveAsync(long restaurantId, long locationId, bool active, CancellationToken ct = default);

        /// <summary>Sets the GST percent and system access fee that the bills of this restaurant use.</summary>
        Task UpdateChargesAsync(long restaurantId, decimal taxPercent, decimal systemAccessFee, CancellationToken ct = default);

        Task<Table> AddTableAsync(long restaurantId, long locationId, string label, int capacity, CancellationToken ct = default);

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
        TimeOnly OpensAt,
        TimeOnly ClosesAt,
        string LocationName,
        string Street,
        string City,
        string State,
        string Country,
        string ZipCode,
        string Phone);

    public sealed record AddLocationRequest(
        string Name,
        string Street,
        string City,
        string State,
        string Country,
        string ZipCode,
        string Phone);
}
