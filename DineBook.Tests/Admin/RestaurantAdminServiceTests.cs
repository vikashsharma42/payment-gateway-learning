using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DineBook.Application.Services;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Exceptions;
using DineBook.Infrastructure.Database;
using Xunit;

namespace DineBook.Tests.Admin
{
    public sealed class RestaurantAdminServiceTests
    {
        private static ApplicationDbContext NewDb() => new ApplicationDbContext(
            new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        private static CreateRestaurantRequest NewRestaurant(string name = "  Lotus Cafe  ") => new CreateRestaurantRequest(
            name, "1 Park Lane", "Sample City", "Sample State", "110001",
            new TimeOnly(9, 0), new TimeOnly(22, 0));

        [Fact]
        public async Task Admin_creates_a_restaurant_with_its_address_and_hours()
        {
            var db = NewDb();
            var admin = new RestaurantAdminService(db);

            var restaurant = await admin.CreateRestaurantAsync(NewRestaurant());

            var saved = await db.Restaurants.SingleAsync();
            Assert.Equal("Lotus Cafe", saved.Name);
            Assert.Equal("1 Park Lane", saved.Address.Street);
            Assert.Equal(new TimeOnly(9, 0), saved.OpensAt);
            Assert.Equal(restaurant.Id, saved.Id);
        }

        [Fact]
        public async Task A_duplicate_restaurant_name_is_refused()
        {
            var db = NewDb();
            var admin = new RestaurantAdminService(db);
            await admin.CreateRestaurantAsync(NewRestaurant("Lotus Cafe"));

            await Assert.ThrowsAsync<DomainException>(() => admin.CreateRestaurantAsync(NewRestaurant("Lotus Cafe")));
        }

        [Fact]
        public async Task Closing_before_opening_is_refused()
        {
            var admin = new RestaurantAdminService(NewDb());
            var bad = new CreateRestaurantRequest("Late Bar", "1 Road", "City", "State", "110001",
                new TimeOnly(22, 0), new TimeOnly(9, 0));

            await Assert.ThrowsAsync<DomainException>(() => admin.CreateRestaurantAsync(bad));
        }

        [Fact]
        public async Task Admin_adds_tables_and_refuses_a_duplicate_label()
        {
            var db = NewDb();
            var admin = new RestaurantAdminService(db);
            var restaurant = await admin.CreateRestaurantAsync(NewRestaurant());

            await admin.AddTableAsync(restaurant.Id, "T1", 4);
            await admin.AddTableAsync(restaurant.Id, "T2", 2);

            var tables = await db.Restaurants.Include(r => r.Tables).SingleAsync();
            Assert.Equal(2, tables.Tables.Count);

            await Assert.ThrowsAsync<DomainException>(() => admin.AddTableAsync(restaurant.Id, "t1", 6));
        }

        [Fact]
        public async Task A_table_needs_a_valid_capacity()
        {
            var admin = new RestaurantAdminService(NewDb());
            var restaurant = await admin.CreateRestaurantAsync(NewRestaurant());

            await Assert.ThrowsAsync<DomainException>(() => admin.AddTableAsync(restaurant.Id, "T1", 0));
        }

        [Fact]
        public async Task Adding_a_table_to_an_unknown_restaurant_is_refused()
        {
            var admin = new RestaurantAdminService(NewDb());

            await Assert.ThrowsAsync<DomainException>(() => admin.AddTableAsync(999, "T1", 2));
        }

        [Fact]
        public async Task Admin_adds_menu_items_and_refuses_a_bad_price()
        {
            var db = NewDb();
            var admin = new RestaurantAdminService(db);
            var restaurant = await admin.CreateRestaurantAsync(NewRestaurant());

            await admin.AddMenuItemAsync(restaurant.Id, "Masala Dosa", "Crisp rice crepe", "Mains", 180m);

            var menu = await admin.GetMenuAsync(restaurant.Id);
            Assert.Equal("Masala Dosa", Assert.Single(menu).Name);

            await Assert.ThrowsAsync<DomainException>(() =>
                admin.AddMenuItemAsync(restaurant.Id, "Free Dosa", "", "Mains", 0m));
        }

        [Fact]
        public async Task Making_an_item_unavailable_keeps_it_in_the_admin_menu()
        {
            var db = NewDb();
            var admin = new RestaurantAdminService(db);
            var restaurant = await admin.CreateRestaurantAsync(NewRestaurant());
            var item = await admin.AddMenuItemAsync(restaurant.Id, "Filter Coffee", "", "Drinks", 60m);

            await admin.SetMenuItemAvailabilityAsync(item.Id, false);

            var menu = await admin.GetMenuAsync(restaurant.Id);
            Assert.False(Assert.Single(menu).IsAvailable);
        }
    }
}
