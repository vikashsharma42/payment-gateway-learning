using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Exceptions;
using DineBook.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using DineBook.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services
{
    public sealed class FoodOrderService : IFoodOrderService
    {
        private readonly ApplicationDbContext _db;
        private readonly IReservationService _reservations;

        public FoodOrderService(ApplicationDbContext db, IReservationService reservations)
        {
            _db = db;
            _reservations = reservations;
        }

        public async Task<Orders> PlaceOrderAsync(
            string reservationCode,
            long userId,
            IReadOnlyList<OrderItemRequest> items,
            CancellationToken ct = default)
        {
            var reservation = await LoadBookingAsync(reservationCode, userId, ct);

            if (items is null || items.Count == 0)
                throw new DomainException("Choose at least one item.");

            // The same item sent twice (for example from two form rows) is merged before validation
            var merged = items
                .GroupBy(i => i.MenuItemId)
                .Select(g => new OrderItemRequest(g.Key, g.Sum(x => x.Quantity)))
                .ToList();

            var ids = merged.Select(m => m.MenuItemId).ToList();

            // Items must belong to this booking's restaurant. Anything else is treated as not found.
            var menu = await _db.MenuItems.AsNoTracking()
                .Where(m => ids.Contains(m.Id) && m.RestaurantId == reservation.RestaurantId)
                .ToDictionaryAsync(m => m.Id, ct);

            var order = new Orders(reservation.RestaurantId);
            foreach (var request in merged)
            {
                // Missing items stay null here; the order reports them as not found
                menu.TryGetValue(request.MenuItemId, out var item);
                order.AddOrderItem(item, request.Quantity);
            }

            // The booking checks that it still accepts food
            reservation.AddFoodOrder(order, _reservations.LocalNow());

            _db.Orders.Add(order);
            await _db.SaveChangesAsync(ct);

            return order;
        }

        public async Task<IReadOnlyList<Orders>> GetOrdersForBookingsAsync(
            IReadOnlyCollection<long> reservationIds, CancellationToken ct = default)
        {
            var ids = (reservationIds ?? Array.Empty<long>()).ToList();
            if (ids.Count == 0)
                return Array.Empty<Orders>();

            return await _db.Orders.AsNoTracking()
                .Include(o => o.Items)
                .Include(o => o.Payments)
                .Where(o => ids.Contains(o.ReservationId) && o.Status != OrderStatus.Cancelled)
                .OrderByDescending(o => o.CreatedDateTime)
                .ToListAsync(ct);
        }

        // Only the account that made the booking can order food for it. Anything else reads as not found.
        private async Task<Reservation> LoadBookingAsync(string code, long userId, CancellationToken ct)
        {
            var reservation = await _reservations.FindByCodeAsync(code, ct);

            if (reservation is null || !reservation.IsBookedBy(userId))
                throw new DomainException("We could not find a booking with those details.");

            return reservation;
        }
    }
}
