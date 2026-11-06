using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using DineBook.Application.Configuration;
using DineBook.Application.Services;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities.Dining;
using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;
using DineBook.Email;
using DineBook.Email.EmailModels;
using DineBook.Infrastructure.Database;
using Xunit;

namespace DineBook.Tests.Dining
{
    // Booking flow against an in-memory database, using the same sample restaurants as the app
    public sealed class ReservationServiceTests
    {
        private const string Phone = "+91 98765 43210";
        private const string CustomerEmail = "asha@example.com";

        private static ApplicationDbContext NewDb() => new ApplicationDbContext(
            new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        private static ReservationService NewService(ApplicationDbContext db, FakeEmailService email) =>
            new ReservationService(
                db,
                Microsoft.Extensions.Options.Options.Create(new DiningOptions()),
                email,
                NullLogger<ReservationService>.Instance);

        private static async Task<(ApplicationDbContext Db, ReservationService Service, FakeEmailService Email)> SetUpAsync()
        {
            var db = NewDb();
            await DiningSeeder.SeedAsync(db);
            var email = new FakeEmailService();
            return (db, NewService(db, email), email);
        }

        private static async Task<Restaurant> RestaurantAsync(ApplicationDbContext db, string name) =>
            await db.Restaurants.Include(r => r.Tables).SingleAsync(r => r.Name == name);

        private static BookingRequest Request(long restaurantId, DateTime startsAt, int guests) => new BookingRequest(
            restaurantId, startsAt, guests, "Asha Verma", Phone, CustomerEmail, string.Empty);

        // A day far enough ahead to pass the notice rule, in restaurant time
        private static DateOnly BookableDay(ReservationService service) =>
            DateOnly.FromDateTime(service.LocalNow()).AddDays(3);

        private static DateTime At(DateOnly day, int hour, int minute = 0) =>
            day.ToDateTime(new TimeOnly(hour, minute));

        private static long TableId(Restaurant restaurant, string label) =>
            restaurant.Tables.Single(t => t.Label == label).Id;

        [Fact]
        public async Task Booking_takes_the_best_fit_table()
        {
            var (db, service, _) = await SetUpAsync();
            var spice = await RestaurantAsync(db, "Spice Route");
            var day = BookableDay(service);

            var reservation = await service.BookAsync(Request(spice.Id, At(day, 19), 3));

            Assert.Equal(TableId(spice, "T3"), reservation.TableId);
            Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        }

        [Fact]
        public async Task A_second_party_at_the_same_time_gets_the_next_table()
        {
            var (db, service, _) = await SetUpAsync();
            var spice = await RestaurantAsync(db, "Spice Route");
            var day = BookableDay(service);

            var first = await service.BookAsync(Request(spice.Id, At(day, 19), 2));
            var second = await service.BookAsync(Request(spice.Id, At(day, 19), 2));

            Assert.Equal(TableId(spice, "T1"), first.TableId);
            Assert.Equal(TableId(spice, "T2"), second.TableId);
        }

        [Fact]
        public async Task A_fully_booked_time_rejects_the_next_party()
        {
            var (db, service, _) = await SetUpAsync();
            var spice = await RestaurantAsync(db, "Spice Route");
            var day = BookableDay(service);

            // Four tables seat two guests or more, so four bookings fit
            for (var i = 0; i < 4; i++)
            {
                await service.BookAsync(Request(spice.Id, At(day, 19), 2));
            }

            await Assert.ThrowsAsync<DomainException>(() =>
                service.BookAsync(Request(spice.Id, At(day, 19), 2)));
        }

        [Fact]
        public async Task A_slot_that_starts_when_another_ends_can_use_the_same_table()
        {
            var (db, service, _) = await SetUpAsync();
            var spice = await RestaurantAsync(db, "Spice Route");
            var day = BookableDay(service);

            // 19:00 with the default 90 minutes ends at 20:30
            var first = await service.BookAsync(Request(spice.Id, At(day, 19), 2));
            var second = await service.BookAsync(Request(spice.Id, At(day, 20, 30), 2));

            Assert.Equal(TableId(spice, "T1"), first.TableId);
            Assert.Equal(TableId(spice, "T1"), second.TableId);
        }

        [Fact]
        public async Task Booking_after_closing_time_is_rejected()
        {
            var (db, service, _) = await SetUpAsync();
            var spice = await RestaurantAsync(db, "Spice Route");
            var day = BookableDay(service);

            // Spice Route closes at 23:30, so 23:45 is outside opening hours
            await Assert.ThrowsAsync<DomainException>(() =>
                service.BookAsync(Request(spice.Id, At(day, 23, 45), 2)));
        }

        [Fact]
        public async Task Party_larger_than_the_limit_is_rejected()
        {
            var (db, service, _) = await SetUpAsync();
            var spice = await RestaurantAsync(db, "Spice Route");
            var day = BookableDay(service);

            await Assert.ThrowsAsync<DomainException>(() =>
                service.BookAsync(Request(spice.Id, At(day, 19), 11)));
        }

        [Fact]
        public async Task Booking_with_too_little_notice_is_rejected()
        {
            var (db, service, _) = await SetUpAsync();
            var spice = await RestaurantAsync(db, "Spice Route");

            await Assert.ThrowsAsync<DomainException>(() =>
                service.BookAsync(Request(spice.Id, service.LocalNow().AddMinutes(10), 2)));
        }

        [Fact]
        public async Task Slots_drop_a_time_once_every_table_for_it_is_booked()
        {
            var (db, service, _) = await SetUpAsync();
            var cafe = await RestaurantAsync(db, "Chai & Co.");
            var day = BookableDay(service);
            var tenAm = At(day, 10);

            var before = await service.GetAvailableSlotsAsync(cafe.Id, day, 2);
            Assert.Contains(tenAm, before);

            // Chai & Co. has two tables that seat two guests
            await service.BookAsync(Request(cafe.Id, tenAm, 2));
            await service.BookAsync(Request(cafe.Id, tenAm, 2));

            var after = await service.GetAvailableSlotsAsync(cafe.Id, day, 2);
            Assert.DoesNotContain(tenAm, after);
        }

        [Fact]
        public async Task Cancelling_frees_the_table_for_others()
        {
            var (db, service, _) = await SetUpAsync();
            var cafe = await RestaurantAsync(db, "Chai & Co.");
            var day = BookableDay(service);
            var tenAm = At(day, 10);

            var first = await service.BookAsync(Request(cafe.Id, tenAm, 2));
            await service.BookAsync(Request(cafe.Id, tenAm, 2));

            await Assert.ThrowsAsync<DomainException>(() =>
                service.CancelAsync(first.Code, "+91 00000 00000"));

            var cancelled = await service.CancelAsync(first.Code, Phone);

            Assert.Equal(ReservationStatus.Cancelled, cancelled.Status);
            Assert.Contains(tenAm, await service.GetAvailableSlotsAsync(cafe.Id, day, 2));
        }

        [Fact]
        public async Task Unknown_booking_code_returns_null()
        {
            var (_, service, _) = await SetUpAsync();

            Assert.Null(await service.FindByCodeAsync("ZZZZZZZZ"));
        }

        [Fact]
        public async Task Booking_sends_a_confirmation_email_to_the_customer()
        {
            var (db, service, email) = await SetUpAsync();
            var spice = await RestaurantAsync(db, "Spice Route");
            var day = BookableDay(service);

            var reservation = await service.BookAsync(Request(spice.Id, At(day, 19), 2));

            var sent = Assert.Single(email.Sent.OfType<ReservationConfirmedEmailModel>());
            Assert.Equal(CustomerEmail, sent.ToAddress);
            Assert.Contains(reservation.Code, sent.Subject);
            Assert.Equal("Spice Route", sent.RestaurantName);
            Assert.Equal(2, sent.Guests);
        }

        [Fact]
        public async Task Cancelling_sends_a_cancellation_email_to_the_customer()
        {
            var (db, service, email) = await SetUpAsync();
            var spice = await RestaurantAsync(db, "Spice Route");
            var day = BookableDay(service);
            var reservation = await service.BookAsync(Request(spice.Id, At(day, 19), 2));

            await service.CancelAsync(reservation.Code, Phone);

            var sent = Assert.Single(email.Sent.OfType<ReservationCancelledEmailModel>());
            Assert.Equal(CustomerEmail, sent.ToAddress);
            Assert.Equal(reservation.Code, sent.ReservationCode);
        }

        [Fact]
        public async Task A_failed_email_does_not_undo_the_booking()
        {
            var (db, service, email) = await SetUpAsync();
            var spice = await RestaurantAsync(db, "Spice Route");
            var day = BookableDay(service);
            email.Fail = true;

            var reservation = await service.BookAsync(Request(spice.Id, At(day, 19), 2));

            Assert.NotNull(await service.FindByCodeAsync(reservation.Code));
        }

        [Fact]
        public async Task A_rejected_booking_sends_no_email()
        {
            var (db, service, email) = await SetUpAsync();
            var spice = await RestaurantAsync(db, "Spice Route");
            var day = BookableDay(service);

            await Assert.ThrowsAsync<DomainException>(() =>
                service.BookAsync(Request(spice.Id, At(day, 23, 45), 2)));

            Assert.Empty(email.Sent);
        }
    }

    // Records emails instead of sending them. Set Fail to simulate a provider error.
    internal sealed class FakeEmailService : IEmailService
    {
        public List<BaseEmailModel> Sent { get; } = new List<BaseEmailModel>();

        public bool Fail { get; set; }

        public Task SendAsync(BaseEmailModel emailModel, CancellationToken ct = default)
        {
            if (Fail)
                throw new InvalidOperationException("Provider rejected the email.");

            Sent.Add(emailModel);
            return Task.CompletedTask;
        }
    }
}
