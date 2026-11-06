using System;
using System.Collections.Generic;
using DineBook.Common.ValueObjects;
using DineBook.Domain.Entities.Dining;
using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;
using Xunit;

namespace DineBook.Tests.Dining
{
    public class ReservationRulesTests
    {
        private static readonly DateTime Evening = new DateTime(2030, 6, 1, 19, 0, 0);

        private static Restaurant NewRestaurant(TimeOnly open, TimeOnly close) =>
            new Restaurant("Test Kitchen", Address.Create("1 Test Road", "Test City", "Test State", "110001"), open, close);

        private static Reservation NewReservation(DateTime startsAt, string phone = "+91 98765 43210") =>
            Reservation.Create(
                "ABCD2345", 1, 1, "Asha Verma", phone, "asha@example.com",
                2, startsAt, 90, string.Empty, DateTimeOffset.UtcNow);

        [Fact]
        public void Overlapping_slots_are_detected()
        {
            Assert.True(ReservationRules.Overlaps(Evening, 90, Evening.AddMinutes(30), 90));
        }

        [Fact]
        public void Back_to_back_slots_do_not_overlap()
        {
            Assert.False(ReservationRules.Overlaps(Evening, 90, Evening.AddMinutes(90), 60));
        }

        [Fact]
        public void Best_fit_picks_the_smallest_table_that_seats_the_party()
        {
            var restaurant = NewRestaurant(new TimeOnly(12, 0), new TimeOnly(23, 0));
            restaurant.AddTable("A", 2);
            restaurant.AddTable("B", 4);
            restaurant.AddTable("C", 6);

            var table = ReservationRules.PickTable(restaurant.Tables, new List<Reservation>(), 3, Evening, 90);

            Assert.Equal("B", table.Label);
        }

        [Fact]
        public void No_table_is_picked_when_the_party_is_too_big()
        {
            var restaurant = NewRestaurant(new TimeOnly(12, 0), new TimeOnly(23, 0));
            restaurant.AddTable("A", 4);

            Assert.Null(ReservationRules.PickTable(restaurant.Tables, new List<Reservation>(), 8, Evening, 90));
        }

        [Fact]
        public void A_booking_must_end_before_closing_time()
        {
            var restaurant = NewRestaurant(new TimeOnly(12, 0), new TimeOnly(23, 30));

            Assert.True(restaurant.IsOpenFor(new DateTime(2030, 6, 1, 22, 0, 0), 90));
            Assert.False(restaurant.IsOpenFor(new DateTime(2030, 6, 1, 22, 30, 0), 90));
        }

        [Fact]
        public void Cancelling_needs_the_matching_phone_number()
        {
            var reservation = NewReservation(Evening);

            Assert.Throws<DomainException>(() => reservation.Cancel("+91 00000 00000"));

            reservation.Cancel("919876543210");

            Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        }

        [Fact]
        public void A_seated_booking_cannot_be_cancelled()
        {
            var reservation = NewReservation(Evening);
            reservation.Seat();

            Assert.Throws<DomainException>(() => reservation.Cancel("919876543210"));
        }

        [Fact]
        public void Guests_are_seated_then_the_booking_is_completed()
        {
            var reservation = NewReservation(Evening);

            reservation.Seat();
            reservation.Complete();

            Assert.Equal(ReservationStatus.Completed, reservation.Status);
        }

        [Fact]
        public void A_booking_code_must_be_eight_characters()
        {
            Assert.Throws<DomainException>(() => Reservation.Create(
                "ABC", 1, 1, "Asha", "9876543210", "asha@example.com", 2, Evening, 90, string.Empty, DateTimeOffset.UtcNow));
        }

        [Fact]
        public void Table_labels_must_be_unique_within_a_restaurant()
        {
            var restaurant = NewRestaurant(new TimeOnly(12, 0), new TimeOnly(23, 0));
            restaurant.AddTable("T1", 2);

            Assert.Throws<DomainException>(() => restaurant.AddTable("t1", 4));
        }
    }
}
