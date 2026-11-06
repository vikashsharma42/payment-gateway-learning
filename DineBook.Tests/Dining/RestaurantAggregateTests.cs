using System;
using DineBook.Common.ValueObjects;
using DineBook.Domain.Entities.Dining;
using DineBook.Domain.Exceptions;
using Xunit;

namespace DineBook.Tests.Dining
{
    public class RestaurantAggregateTests
    {
        private static readonly DateTime Evening = new DateTime(2030, 6, 1, 19, 0, 0);
        private static readonly DateTimeOffset Now = new DateTimeOffset(2030, 6, 1, 10, 0, 0, TimeSpan.Zero);

        private static Restaurant NewRestaurant(string name = "Test Kitchen") =>
            new Restaurant(name, Address.Create("1 Test Road", "Test City", "Test State", "110001"), new TimeOnly(12, 0), new TimeOnly(23, 0));

        [Fact]
        public void A_table_can_be_renamed_and_the_label_is_trimmed()
        {
            var restaurant = NewRestaurant();
            var table = restaurant.AddTable("T1", 2);

            restaurant.RenameTable(table, "  Corner  ");

            Assert.Equal("Corner", table.Label);
        }

        [Fact]
        public void Renaming_to_another_tables_label_is_refused_ignoring_case()
        {
            var restaurant = NewRestaurant();
            restaurant.AddTable("T1", 2);
            var second = restaurant.AddTable("T2", 4);

            Assert.Throws<DomainException>(() => restaurant.RenameTable(second, "t1"));
            Assert.Equal("T2", second.Label);
        }

        [Fact]
        public void A_removed_table_leaves_the_restaurant_and_its_label_can_be_reused()
        {
            var restaurant = NewRestaurant();
            var table = restaurant.AddTable("T1", 2);

            restaurant.RemoveTable(table, Now);

            Assert.DoesNotContain(table, restaurant.Tables);
            Assert.True(table.IsDeleted);
            Assert.Equal(Now, table.DeletedAt);

            var replacement = restaurant.AddTable("T1", 4);
            Assert.Equal(4, replacement.Capacity);
        }

        [Fact]
        public void A_table_from_another_restaurant_cannot_be_removed()
        {
            var owner = NewRestaurant("Owner");
            var other = NewRestaurant("Other");
            var table = owner.AddTable("T1", 2);

            Assert.Throws<DomainException>(() => other.RemoveTable(table, Now));
        }

        [Fact]
        public void Menu_items_are_linked_to_their_restaurant_and_can_be_removed()
        {
            var restaurant = NewRestaurant();
            var item = restaurant.AddMenuItem("Dal", "Lentils", "Mains", 320m);

            Assert.Same(restaurant, item.Restaurant);
            Assert.Contains(item, restaurant.MenuItems);

            item.Delete(Now);

            Assert.DoesNotContain(item, restaurant.MenuItems);
            Assert.False(item.IsAvailable);
        }

        [Fact]
        public void A_cancelled_booking_refuses_food_orders()
        {
            var restaurant = NewRestaurant();
            var item = restaurant.AddMenuItem("Dal", "Lentils", "Mains", 320m);
            var reservation = Reservation.Create(
                "ABCD2345", 1, 1, "Asha Verma", "+91 98765 43210", "asha@example.com",
                2, Evening, 90, string.Empty, Now);
            reservation.Cancel("919876543210");

            Assert.Throws<DomainException>(() =>
                reservation.PlaceFoodOrder(new[] { (item, 1) }, Evening, Now));
        }
    }
}
