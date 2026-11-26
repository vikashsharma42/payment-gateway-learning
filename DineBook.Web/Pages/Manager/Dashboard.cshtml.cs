using DineBook.Application.Services.Interface;
using DineBook.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Manager
{
    public class DashboardModel : ManagerPageModel
    {
        private readonly IStaffWorkService _work;
        private readonly IReservationService _reservations;

        public DashboardModel(
            IStaffLocationService staffLocations,
            IRestaurantAdminService admin,
            IStaffWorkService work,
            IReservationService reservations)
            : base(staffLocations, admin)
        {
            _work = work;
            _reservations = reservations;
        }

        public int OpenLocationCount { get; private set; }
        public int TableCount { get; private set; }
        public int DishCount { get; private set; }
        public int TodayBookingCount { get; private set; }
        public int PlacedOrderCount { get; private set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            await LoadWorkspaceAsync(null, ct);
            if (Workspace is null)
                return Page();

            var restaurantId = Workspace.Restaurant.Id;
            var today = _reservations.LocalNow().Date;

            OpenLocationCount = Workspace.Locations.Count;
            TableCount = Workspace.Restaurant.Tables.Count;
            DishCount = (await Admin.GetMenuAsync(restaurantId, ct)).Count;

            var bookings = await _work.GetRestaurantReservationsAsync(restaurantId, today, ct);
            TodayBookingCount = bookings.Count(b => b.StartsAt.Date == today);

            var orders = await _work.GetRestaurantOrdersAsync(restaurantId, ct);
            PlacedOrderCount = orders.Count(o => o.Status == OrderStatus.Placed);

            return Page();
        }
    }
}
