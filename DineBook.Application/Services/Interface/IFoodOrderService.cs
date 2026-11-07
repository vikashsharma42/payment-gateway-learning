using DineBook.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services.Interface
{
    public interface IFoodOrderService
    {
        /// <summary>
        /// Places one order with many items for an active booking. The customer must give the booking's phone number.
        /// </summary>
        Task<Orders> PlaceOrderAsync(
            string reservationCode,
            string phone,
            IReadOnlyList<OrderItemRequest> items,
            CancellationToken ct = default);

        Task<IReadOnlyList<Orders>> GetOrdersAsync(string reservationCode, string phone, CancellationToken ct = default);

        /// <summary>Food orders (not cancelled) for these bookings, with their items and payments. Newest first.</summary>
        Task<IReadOnlyList<Orders>> GetOrdersForBookingsAsync(IReadOnlyCollection<long> reservationIds, CancellationToken ct = default);
    }

    public sealed record OrderItemRequest(long MenuItemId, int Quantity);
}
