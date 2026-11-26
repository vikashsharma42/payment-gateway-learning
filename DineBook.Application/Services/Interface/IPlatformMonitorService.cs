using DineBook.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services.Interface
{
    /// <summary>SuperAdmin view across every restaurant: counts and the most recent bookings, orders and payments.</summary>
    public interface IPlatformMonitorService
    {
        /// <summary>Platform-wide counts. "Bookings today" counts bookings that start on the given day.</summary>
        Task<PlatformStats> GetStatsAsync(DateTime day, CancellationToken ct = default);

        /// <summary>Most recent bookings across all restaurants. Restaurant is loaded.</summary>
        Task<IReadOnlyList<Reservation>> GetRecentReservationsAsync(CancellationToken ct = default);

        /// <summary>Most recent food orders across all restaurants. Restaurant, items and payments are loaded.</summary>
        Task<IReadOnlyList<Orders>> GetRecentOrdersAsync(CancellationToken ct = default);

        /// <summary>Most recent payments and refunds across all restaurants. Order and restaurant are loaded.</summary>
        Task<IReadOnlyList<Payment>> GetRecentPaymentsAsync(CancellationToken ct = default);
    }

    public sealed record PlatformStats(
        int Restaurants,
        int ActiveRestaurants,
        int Users,
        int Customers,
        int BookingsToday,
        int PlacedOrders);
}
