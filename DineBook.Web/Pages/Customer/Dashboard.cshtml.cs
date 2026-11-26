using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Customer
{
    public class DashboardModel : AppPageModel
    {
        private readonly ICustomerService _customers;
        private readonly IReservationService _reservations;

        public DashboardModel(ICustomerService customers, IReservationService reservations)
        {
            _customers = customers;
            _reservations = reservations;
        }

        public ApplicationUser Profile { get; private set; }

        public IReadOnlyList<Reservation> UpcomingBookings { get; private set; }

        public IReadOnlyList<Orders> RecentOrders { get; private set; }

        public int UnpaidOrderCount { get; private set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            Profile = await _customers.GetProfileAsync(UserId, ct);

            var now = _reservations.LocalNow();
            var bookings = await _customers.GetReservationsAsync(UserId, ct);
            UpcomingBookings = bookings
                .Where(b => b.Status == ReservationStatus.Confirmed && b.StartsAt >= now)
                .OrderBy(b => b.StartsAt)
                .Take(5)
                .ToList();

            var orders = await _customers.GetOrdersAsync(UserId, ct);
            RecentOrders = orders.Take(5).ToList();
            UnpaidOrderCount = orders.Count(o => o.Status != OrderStatus.Cancelled && !o.IsPaid);

            return Page();
        }
    }
}
