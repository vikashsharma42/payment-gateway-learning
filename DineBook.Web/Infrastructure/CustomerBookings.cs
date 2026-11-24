using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Infrastructure
{
    /// <summary>The bookings of one signed-in customer, each with its food orders. Used by My bookings and Order history.</summary>
    public sealed class CustomerBookings
    {
        private readonly IReservationService _reservations;
        private readonly IFoodOrderService _orders;
        private readonly BillingQueries _billing;

        public CustomerBookings(IReservationService reservations, IFoodOrderService orders, BillingQueries billing)
        {
            _reservations = reservations;
            _orders = orders;
            _billing = billing;
        }

        public async Task<IReadOnlyList<BookingWithOrders>> LoadAsync(long userId, CancellationToken ct = default)
        {
            var bookings = await _reservations.GetBookingsForUserAsync(userId, ct);
            if (bookings.Count == 0)
                return Array.Empty<BookingWithOrders>();

            var orders = await _orders.GetOrdersForBookingsAsync(bookings.Select(b => b.Id).ToList(), ct);

            var result = new List<BookingWithOrders>();
            foreach (var booking in bookings)
            {
                // One database read per restaurant: its GST and system access fee
                var charges = await _billing.GetChargesAsync(booking.RestaurantId, ct);
                result.Add(new BookingWithOrders(booking, orders.Where(o => o.ReservationId == booking.Id).ToList(), charges));
            }

            return result;
        }
    }

    /// <summary>A booking with its food orders, for the bookings and order history pages.</summary>
    public sealed record BookingWithOrders(Reservation Booking, IReadOnlyList<Orders> FoodOrders, RestaurantCharges Charges)
    {
        public IReadOnlyList<Orders> UnpaidOrders => FoodOrders.Where(o => !o.IsPaid && o.Status != OrderStatus.Cancelled).ToList();

        // The bill (items + system access fee + GST) of one food order
        public Bill BillFor(Orders order) => BillCalculator.Calculate(order.Subtotal(), Charges);

        public decimal UnpaidTotal => UnpaidOrders.Sum(o => BillFor(o).Total);
    }
}
