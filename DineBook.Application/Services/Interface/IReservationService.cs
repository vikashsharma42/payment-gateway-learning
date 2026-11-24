using DineBook.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services.Interface
{
    public interface IReservationService
    {
        /// <summary>Current time in the restaurant time zone.</summary>
        DateTime LocalNow();

        Task<IReadOnlyList<Restaurant>> GetRestaurantsAsync(CancellationToken ct = default);

        /// <summary>Restaurant with its tables. Null when it does not exist.</summary>
        Task<Restaurant> GetRestaurantAsync(long restaurantId, CancellationToken ct = default);

        /// <summary>Restaurant with its tables, found by its web address (slug). Null when none has it.</summary>
        Task<Restaurant> GetRestaurantBySlugAsync(string slug, CancellationToken ct = default);

        /// <summary>Start times on a day that still have a free table for the party size.</summary>
        Task<IReadOnlyList<DateTime>> GetAvailableSlotsAsync(long restaurantId, DateOnly date, int guests, CancellationToken ct = default);

        /// <summary>Books the best-fit free table for the signed-in customer. Throws DomainException when a rule is broken.</summary>
        Task<Reservation> BookAsync(BookingRequest request, CancellationToken ct = default);

        /// <summary>Null when no booking has this code.</summary>
        Task<Reservation> FindByCodeAsync(string code, CancellationToken ct = default);

        /// <summary>True when the booking belongs to the customer linked to this account.</summary>
        Task<bool> IsOwnedByAsync(long reservationId, long userId, CancellationToken ct = default);

        /// <summary>Cancels a confirmed booking that belongs to this account.</summary>
        Task<Reservation> CancelAsync(string code, long userId, CancellationToken ct = default);

        /// <summary>Every booking of the customer linked to this account. Newest first.</summary>
        Task<IReadOnlyList<Reservation>> GetBookingsForUserAsync(long userId, CancellationToken ct = default);
    }

    public sealed record BookingRequest(
        long ApplicationUserId,
        long RestaurantId,
        DateTime StartsAt,
        int Guests,
        string CustomerName,
        string CustomerPhone,
        string CustomerEmail,
        string SpecialRequests);
}
