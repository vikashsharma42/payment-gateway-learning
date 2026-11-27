using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Exceptions;
using DineBook.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
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

        public async Task<Orders> PlaceOrderAsync(string reservationCode, string phone, IReadOnlyList<OrderItemRequest> items, CancellationToken ct = default)
        {
            var reservation = await LoadBookingAsync(reservationCode, phone, ct);

            if (items is null || items.Count == 0)
                throw new DomainException("Choose at least one item.");

            if (items.Any(i => i.Quantity <= 0))
                throw new DomainException("Quantity must be greater than zero.");

            // The same item sent twice (for example from two form rows) is merged before validation
            var merged = items
                .GroupBy(i => i.MenuItemId)
                .Select(g => new OrderItemRequest(g.Key, g.Sum(x => x.Quantity)))
                .ToList();

            var ids = merged.Select(m => m.MenuItemId).ToList();

            // Items must belong to this booking's restaurant. Anything else is treated as not found.
            var menu = await _db.MenuItems.AsNoTracking()
                .Where(m => ids.Contains(m.Id) && m.RestaurantId == reservation.RestaurantId && m.IsAvailable)
                .ToDictionaryAsync(m => m.Id, ct);

            var order = new Orders();

            // The booking checks that it still accepts food and sets the order's restaurant and reservation.
            // This must run before the items are added, because Orders.AddOrderItem checks each item
            // against the order's restaurant.
            reservation.AddFoodOrder(order, _reservations.LocalNow());

            foreach (var request in merged)
            {
                if (!menu.TryGetValue(request.MenuItemId, out var item))
                    throw new DomainException("One or more selected menu items are unavailable.");

                order.AddOrderItem(item, request.Quantity);
            }

            _db.Orders.Add(order);
            await _db.SaveChangesAsync(ct);

            return order;
        }

        public async Task<IReadOnlyList<Orders>> GetOrdersAsync(string reservationCode, string phone, CancellationToken ct = default)
        {
            var reservation = await LoadBookingAsync(reservationCode, phone, ct);

            return await _db.Orders.AsNoTracking()
                .Where(o => o.ReservationId == reservation.Id)
                .Include(o => o.Items)
                .OrderBy(o => o.CreatedDateTime)
                .ToListAsync(ct);
        }

        public async Task MarkServedAsync(long restaurantId, long orderId, CancellationToken ct = default)
        {
            var order = await LoadStaffOrderAsync(restaurantId, orderId, ct);

            order.MarkServed();
            await _db.SaveChangesAsync(ct);
        }

        public async Task CancelOrderAsync(long restaurantId, long orderId, CancellationToken ct = default)
        {
            var order = await LoadStaffOrderAsync(restaurantId, orderId, ct);

            order.Cancel();
            await _db.SaveChangesAsync(ct);
        }

        private async Task<Orders> LoadStaffOrderAsync(long restaurantId, long orderId, CancellationToken ct) =>
            await _db.Orders.Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.RestaurantId == restaurantId, ct)
            ?? throw new DomainException("Order not found.");

        private async Task<Reservation> LoadBookingAsync(string code, string phone, CancellationToken ct)
        {
            var normalizedCode = (code ?? string.Empty).Trim().ToUpperInvariant();
            var normalizedPhone = ApplicationUser.NormalizePhone(phone);

            if (string.IsNullOrWhiteSpace(normalizedCode) || string.IsNullOrWhiteSpace(normalizedPhone))
                throw new DomainException("We couldn't find a booking with that code and phone number. Please check them and try again.");

            // The customer is needed to check the phone number. FindByCodeAsync does not load it.
            var reservation = await _db.Reservations.AsNoTracking()
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.Code == normalizedCode, ct);

            // Same message for an unknown code and a wrong phone, so the code alone reveals nothing
            if (reservation?.Customer is null || reservation.Customer.Phone != normalizedPhone)
                throw new DomainException("We couldn't find a booking with that code and phone number. Please check them and try again.");

            return reservation;
        }
    }
}
