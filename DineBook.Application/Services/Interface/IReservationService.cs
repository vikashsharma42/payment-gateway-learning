using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DineBook.Domain.Entities.Dining;
using DineBook.Domain.Entities.Organization;

namespace DineBook.Application.Services.Interface
{
    public interface IReservationService
    {
        /// <summary>Current time in the restaurant time zone.</summary>
        DateTime LocalNow();

        Task<IReadOnlyList<Restaurant>> GetRestaurantsAsync(CancellationToken ct = default);

        /// <summary>Restaurant with its tables. Null when it does not exist.</summary>
        Task<Restaurant> GetRestaurantAsync(long restaurantId, CancellationToken ct = default);

        /// <summary>Start times on a day that still have a free table for the party size.</summary>
        Task<IReadOnlyList<DateTime>> GetAvailableSlotsAsync(long restaurantId, DateOnly date, int guests, CancellationToken ct = default);

        /// <summary>Books the best-fit free table. Throws DomainException when the request breaks a rule.</summary>
        Task<Reservation> BookAsync(BookingRequest request, CancellationToken ct = default);

        /// <summary>Null when no booking has this code.</summary>
        Task<Reservation> FindByCodeAsync(string code, CancellationToken ct = default);

        /// <summary>Cancels a confirmed booking. The phone number must match the booking.</summary>
        Task<Reservation> CancelAsync(string code, string phone, CancellationToken ct = default);
    }

    public sealed record BookingRequest(
        long RestaurantId,
        DateTime StartsAt,
        int Guests,
        string CustomerName,
        string CustomerPhone,
        string CustomerEmail,
        string SpecialRequests);
}
