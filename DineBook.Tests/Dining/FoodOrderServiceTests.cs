using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using DineBook.Application.Configuration;
using DineBook.Application.Services;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities.Dining;
using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;
using DineBook.Infrastructure.Database;
using Xunit;

namespace DineBook.Tests.Dining
{
    // Food ordering against an in-memory database, using the same sample data as the app
    public sealed class FoodOrderServiceTests
    {
        private const string Phone = "+91 98765 43210";

        private static ApplicationDbContext NewDb() => new ApplicationDbContext(
            new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        private static async Task<(ApplicationDbContext Db, ReservationService Reservations, FoodOrderService Orders)> SetUpAsync()
        {
            var db = NewDb();
            await DiningSeeder.SeedAsync(db);
            var reservations = new ReservationService(
                db,
                Microsoft.Extensions.Options.Options.Create(new DiningOptions()),
                new FakeEmailService(),
                NullLogger<ReservationService>.Instance);
            return (db, reservations, new FoodOrderService(db, reservations));
        }

        private static async Task<long> RestaurantIdAsync(ApplicationDbContext db, string name) =>
            (await db.Restaurants.SingleAsync(r => r.Name == name)).Id;

        private static async Task<long> ItemIdAsync(ApplicationDbContext db, long restaurantId, string name) =>
            (await db.MenuItems.SingleAsync(m => m.RestaurantId == restaurantId && m.Name == name)).Id;

        private static async Task<string> BookAsync(ReservationService reservations, long restaurantId)
        {
            var day = DateOnly.FromDateTime(reservations.LocalNow()).AddDays(3);
            var booking = await reservations.BookAsync(new BookingRequest(
                restaurantId, day.ToDateTime(new TimeOnly(19, 0)), 2, "Asha Verma", Phone, "asha@example.com", string.Empty));
            return booking.Code;
        }

        [Fact]
        public async Task Many_items_are_ordered_together_in_one_order()
        {
            var (db, reservations, orders) = await SetUpAsync();
            var spice = await RestaurantIdAsync(db, "Spice Route");
            var code = await BookAsync(reservations, spice);

            var order = await orders.PlaceOrderAsync(code, Phone, new[]
            {
                new OrderItemRequest(await ItemIdAsync(db, spice, "Dal Makhani"), 2),
                new OrderItemRequest(await ItemIdAsync(db, spice, "Garlic Naan"), 4),
                new OrderItemRequest(await ItemIdAsync(db, spice, "Sweet Lassi"), 1),
            });

            Assert.Equal(3, order.Lines.Count);
            Assert.Equal(2 * 320m + 4 * 60m + 120m, order.Total());
            Assert.Equal(FoodOrderStatus.Placed, order.Status);
        }

        [Fact]
        public async Task The_same_item_sent_twice_is_merged_into_one_line()
        {
            var (db, reservations, orders) = await SetUpAsync();
            var spice = await RestaurantIdAsync(db, "Spice Route");
            var code = await BookAsync(reservations, spice);
            var naan = await ItemIdAsync(db, spice, "Garlic Naan");

            var order = await orders.PlaceOrderAsync(code, Phone, new[]
            {
                new OrderItemRequest(naan, 1),
                new OrderItemRequest(naan, 2),
            });

            var line = Assert.Single(order.Lines);
            Assert.Equal(3, line.Quantity);
        }

        [Fact]
        public async Task A_wrong_phone_number_is_refused()
        {
            var (db, reservations, orders) = await SetUpAsync();
            var spice = await RestaurantIdAsync(db, "Spice Route");
            var code = await BookAsync(reservations, spice);

            var naan = await ItemIdAsync(db, spice, "Garlic Naan");

            await Assert.ThrowsAsync<DomainException>(() => orders.PlaceOrderAsync(code, "+91 00000 00000",
                new[] { new OrderItemRequest(naan, 1) }));
        }

        [Fact]
        public async Task Items_from_another_restaurant_are_refused()
        {
            var (db, reservations, orders) = await SetUpAsync();
            var spice = await RestaurantIdAsync(db, "Spice Route");
            var chai = await RestaurantIdAsync(db, "Chai & Co.");
            var code = await BookAsync(reservations, spice);

            var chaiItem = await ItemIdAsync(db, chai, "Masala Chai");

            await Assert.ThrowsAsync<DomainException>(() => orders.PlaceOrderAsync(code, Phone,
                new[] { new OrderItemRequest(chaiItem, 1) }));
        }

        [Fact]
        public async Task Unavailable_items_are_refused()
        {
            var (db, reservations, orders) = await SetUpAsync();
            var spice = await RestaurantIdAsync(db, "Spice Route");
            var code = await BookAsync(reservations, spice);
            var dal = await db.MenuItems.SingleAsync(m => m.RestaurantId == spice && m.Name == "Dal Makhani");
            dal.SetAvailability(false);
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<DomainException>(() => orders.PlaceOrderAsync(code, Phone,
                new[] { new OrderItemRequest(dal.Id, 1) }));
        }

        [Fact]
        public async Task A_cancelled_booking_cannot_take_food_orders()
        {
            var (db, reservations, orders) = await SetUpAsync();
            var spice = await RestaurantIdAsync(db, "Spice Route");
            var code = await BookAsync(reservations, spice);
            await reservations.CancelAsync(code, Phone);
            var naan = await ItemIdAsync(db, spice, "Garlic Naan");

            await Assert.ThrowsAsync<DomainException>(() => orders.PlaceOrderAsync(code, Phone,
                new[] { new OrderItemRequest(naan, 1) }));
        }

        [Fact]
        public async Task Orders_are_listed_for_the_booking_and_need_the_phone_number()
        {
            var (db, reservations, orders) = await SetUpAsync();
            var spice = await RestaurantIdAsync(db, "Spice Route");
            var code = await BookAsync(reservations, spice);

            await orders.PlaceOrderAsync(code, Phone, new[] { new OrderItemRequest(await ItemIdAsync(db, spice, "Garlic Naan"), 1) });
            await orders.PlaceOrderAsync(code, Phone, new[] { new OrderItemRequest(await ItemIdAsync(db, spice, "Sweet Lassi"), 2) });

            Assert.Equal(2, (await orders.GetOrdersAsync(code, Phone)).Count);
            await Assert.ThrowsAsync<DomainException>(() => orders.GetOrdersAsync(code, "+91 00000 00000"));
        }
    }
}
