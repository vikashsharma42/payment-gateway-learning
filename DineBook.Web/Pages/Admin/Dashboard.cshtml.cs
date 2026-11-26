using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Web.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Admin
{
    // Platform-wide view for the SuperAdmin: counts and the most recent activity
    public class DashboardModel : AppPageModel
    {
        private readonly IPlatformMonitorService _monitor;
        private readonly IReservationService _reservations;

        public DashboardModel(IPlatformMonitorService monitor, IReservationService reservations)
        {
            _monitor = monitor;
            _reservations = reservations;
        }

        public PlatformStats Stats { get; private set; }

        public IReadOnlyList<Reservation> RecentBookings { get; private set; }

        public IReadOnlyList<Orders> RecentOrders { get; private set; }

        public IReadOnlyList<Payment> RecentPayments { get; private set; }

        public async Task OnGetAsync(CancellationToken ct)
        {
            Stats = await _monitor.GetStatsAsync(_reservations.LocalNow(), ct);
            RecentBookings = (await _monitor.GetRecentReservationsAsync(ct)).Take(10).ToList();
            RecentOrders = (await _monitor.GetRecentOrdersAsync(ct)).Take(10).ToList();
            RecentPayments = (await _monitor.GetRecentPaymentsAsync(ct)).Take(10).ToList();
        }
    }
}
