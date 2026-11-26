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

        /// <summary>Start times on a day that still have a free table for the party size.</summary>
        Task<IReadOnlyList<DateTime>> GetAvailableSlotsAsync(long restaurantId, DateOnly date, int guests, CancellationToken ct = default);

        /// <summary>Books the best-fit free table for a registered customer. Throws DomainException when the request breaks a rule.</summary>
        Task<Reservation> BookAsync(BookingRequest request, CancellationToken ct = default);

        /// <summary>Null when no booking has this code.</summary>
        Task<Reservation> FindByCodeAsync(string code, CancellationToken ct = default);

        /// <summary>Cancels a confirmed booking. The phone number must match the booking's customer.</summary>
        Task<Reservation> CancelAsync(string code, string phone, CancellationToken ct = default);

        /// <summary>Staff: guests have arrived. The table becomes occupied.</summary>
        Task SeatAsync(long restaurantId, long reservationId, CancellationToken ct = default);

        /// <summary>Staff: the visit is over. The table is released.</summary>
        Task CompleteAsync(long restaurantId, long reservationId, CancellationToken ct = default);

        /// <summary>Staff: the guests did not arrive.</summary>
        Task MarkNoShowAsync(long restaurantId, long reservationId, CancellationToken ct = default);
    }

    /// <summary>CustomerId is the ApplicationUser (Customer role) who is booking.</summary>
    public sealed record BookingRequest(
        long RestaurantId,
        long CustomerId,
        DateTime StartsAt,
        int Guests,
        string SpecialRequests);
}
