using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Exceptions;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Staff
{
    // Today's and upcoming bookings at the staff member's current location
    [Authorize(Roles = "Staff,Manager")]
    public class ReservationsModel : AppPageModel
    {
        private readonly IStaffLocationService _staffLocations;
        private readonly IStaffWorkService _work;
        private readonly IReservationService _reservations;

        public ReservationsModel(IStaffLocationService staffLocations, IStaffWorkService work, IReservationService reservations)
        {
            _staffLocations = staffLocations;
            _work = work;
            _reservations = reservations;
        }

        public StaffWorkspace Workspace { get; private set; }

        public IReadOnlyList<Reservation> Bookings { get; private set; } = Array.Empty<Reservation>();

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            Workspace = await _staffLocations.GetWorkspaceAsync(UserId, ReadCookieLocationId(), ct);
            if (Workspace is not null)
            {
                var today = _reservations.LocalNow().Date;
                Bookings = await _work.GetReservationsAsync(Workspace.Restaurant.Id, Workspace.Current.Id, today, ct);
            }

            return Page();
        }

        public Task<IActionResult> OnPostSeatAsync(long reservationId, CancellationToken ct) =>
            ActAsync(reservationId, restaurantId => _reservations.SeatAsync(restaurantId, reservationId, ct), "Guests are seated.", ct);

        public Task<IActionResult> OnPostCompleteAsync(long reservationId, CancellationToken ct) =>
            ActAsync(reservationId, restaurantId => _reservations.CompleteAsync(restaurantId, reservationId, ct), "Booking completed. The table is free.", ct);

        public Task<IActionResult> OnPostNoShowAsync(long reservationId, CancellationToken ct) =>
            ActAsync(reservationId, restaurantId => _reservations.MarkNoShowAsync(restaurantId, reservationId, ct), "Marked as no-show.", ct);

        // Checks the booking is at this location, runs the action, then shows the result
        private async Task<IActionResult> ActAsync(long reservationId, Func<long, Task> action, string success, CancellationToken ct)
        {
            try
            {
                var workspace = await _staffLocations.GetWorkspaceAsync(UserId, ReadCookieLocationId(), ct)
                    ?? throw new DomainException("You are not assigned to a restaurant.");

                await _work.EnsureReservationAtLocationAsync(workspace.Restaurant.Id, workspace.Current.Id, reservationId, ct);
                await action(workspace.Restaurant.Id);
                ToastSuccess(success);
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage();
        }

        private long? ReadCookieLocationId() =>
            long.TryParse(Request.Cookies[DashboardModel.LocationCookie], out var id) ? id : null;
    }
}
