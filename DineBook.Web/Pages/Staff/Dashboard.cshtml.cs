using DineBook.Application.Services.Interface;
using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;
using DineBook.Web.Dtos;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Staff
{
    // Staff and managers work at one restaurant and switch between its locations here
    [Authorize(Roles = "Staff,Manager")]
    public class DashboardModel : AppPageModel
    {
        // The chosen location is kept in this cookie. It is checked against the user's restaurant on every request.
        public const string LocationCookie = "DineBook.Location";

        private readonly IStaffLocationService _staffLocations;
        private readonly IStaffWorkService _work;
        private readonly IReservationService _reservations;

        public DashboardModel(IStaffLocationService staffLocations, IStaffWorkService work, IReservationService reservations)
        {
            _staffLocations = staffLocations;
            _work = work;
            _reservations = reservations;
        }

        public StaffWorkspace Workspace { get; private set; }

        [BindProperty]
        public LocationSwitchDto Input { get; set; } = new();

        public int TodayBookingCount { get; private set; }

        public int PlacedOrderCount { get; private set; }

        public async Task<IActionResult> OnGetAsync(long? locationId, CancellationToken ct)
        {
            Workspace = await _staffLocations.GetWorkspaceAsync(UserId, locationId ?? ReadCookieLocationId(), ct);

            // The user has no restaurant (or no open location): the page explains that
            if (Workspace is not null)
            {
                Input.LocationId = Workspace.Current.Id;

                var today = _reservations.LocalNow().Date;
                var bookings = await _work.GetReservationsAsync(Workspace.Restaurant.Id, Workspace.Current.Id, today, ct);
                TodayBookingCount = bookings.Count(b => b.StartsAt.Date == today);

                var orders = await _work.GetOrdersAsync(Workspace.Restaurant.Id, Workspace.Current.Id, ct);
                PlacedOrderCount = orders.Count(o => o.Status == OrderStatus.Placed);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSwitchAsync(CancellationToken ct)
        {
            try
            {
                var workspace = await _staffLocations.GetWorkspaceAsync(UserId, Input.LocationId, ct);
                if (workspace is null)
                    throw new DomainException("You are not assigned to a restaurant.");

                // Only an open location of this user's restaurant can be chosen
                if (workspace.Current.Id != Input.LocationId)
                    throw new DomainException("That location is not available.");

                Response.Cookies.Append(LocationCookie, Input.LocationId.ToString(CultureInfo.InvariantCulture), new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    IsEssential = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(30)
                });

                ToastSuccess($"Now working at {workspace.Current.Name}.");
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage();
        }

        private long? ReadCookieLocationId() =>
            long.TryParse(Request.Cookies[LocationCookie], out var id) ? id : null;
    }
}