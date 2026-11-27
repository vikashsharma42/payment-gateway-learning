using DineBook.Application.Paging;
using DineBook.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services.Interface
{
    /// <summary>
    /// Staff view of one location: its bookings and incoming food orders.
    /// Every query is limited to the restaurant and location passed in. The Ensure methods check a record
    /// belongs to that location before staff act on it.
    /// </summary>
    public interface IStaffWorkService
    {
        /// <summary>Bookings at the location from the given moment onwards, earliest first. Customer and table are loaded.</summary>
        Task<IReadOnlyList<Reservation>> GetReservationsAsync(long restaurantId, long locationId, DateTime from, CancellationToken ct = default);

        /// <summary>Food orders for bookings at the location, newest first. Items and reservation are loaded.</summary>
        Task<IReadOnlyList<Orders>> GetOrdersAsync(long restaurantId, long locationId, CancellationToken ct = default);

        /// <summary>Bookings at every location of the restaurant from the given moment onwards, earliest first.</summary>
        Task<IReadOnlyList<Reservation>> GetRestaurantReservationsAsync(long restaurantId, DateTime from, CancellationToken ct = default);

        /// <summary>Food orders at every location of the restaurant, newest first.</summary>
        Task<IReadOnlyList<Orders>> GetRestaurantOrdersAsync(long restaurantId, CancellationToken ct = default);

        /// <summary>One page of bookings at the location from the given moment, earliest first.</summary>
        Task<PagedResult<Reservation>> GetReservationsPageAsync(long restaurantId, long locationId, DateTime from, int page, int pageSize, CancellationToken ct = default);

        /// <summary>One page of food orders for bookings at the location, newest first.</summary>
        Task<PagedResult<Orders>> GetOrdersPageAsync(long restaurantId, long locationId, int page, int pageSize, CancellationToken ct = default);

        /// <summary>One page of bookings at every location of the restaurant from the given moment, earliest first.</summary>
        Task<PagedResult<Reservation>> GetRestaurantReservationsPageAsync(long restaurantId, DateTime from, int page, int pageSize, CancellationToken ct = default);

        /// <summary>One page of food orders at every location of the restaurant, newest first.</summary>
        Task<PagedResult<Orders>> GetRestaurantOrdersPageAsync(long restaurantId, int page, int pageSize, CancellationToken ct = default);

        /// <summary>Throws DomainException when the booking is not at this location of this restaurant.</summary>
        Task EnsureReservationAtLocationAsync(long restaurantId, long locationId, long reservationId, CancellationToken ct = default);

        /// <summary>Throws DomainException when the order is not for a booking at this location of this restaurant.</summary>
        Task EnsureOrderAtLocationAsync(long restaurantId, long locationId, long orderId, CancellationToken ct = default);
    }
}
