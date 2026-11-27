using DineBook.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services.Interface
{
    /// <summary>
    /// The signed-in customer's own account: profile, bookings, food orders and payments.
    /// Every query is limited to the customer id passed in, so a customer only sees their own records.
    /// </summary>
    public interface ICustomerService
    {
        Task<ApplicationUser> GetProfileAsync(long customerId, CancellationToken ct = default);

        Task UpdateProfileAsync(long customerId, string firstName, string lastName, string phone, CancellationToken ct = default);

        /// <summary>Bookings, newest first. Restaurant is loaded.</summary>
        Task<IReadOnlyList<Reservation>> GetReservationsAsync(long customerId, CancellationToken ct = default);

        /// <summary>Food orders, newest first. Restaurant, reservation and items are loaded.</summary>
        Task<IReadOnlyList<Orders>> GetOrdersAsync(long customerId, CancellationToken ct = default);

        /// <summary>Food orders at one restaurant, newest first. Restaurant, reservation and items are loaded.</summary>
        Task<IReadOnlyList<Orders>> GetRestaurantOrdersAsync(long customerId, long restaurantId, CancellationToken ct = default);

        /// <summary>Payments and refunds for the customer's orders, newest first. Order and restaurant are loaded.</summary>
        Task<IReadOnlyList<Payment>> GetPaymentsAsync(long customerId, CancellationToken ct = default);
        /// <summary>The account with this id, or null when there is none. Used to pre-fill forms.</summary>
        Task<ApplicationUser> FindProfileAsync(long customerId, CancellationToken ct = default);
    }
}
