using DineBook.Application.Paging;
using DineBook.Web.Dtos;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Manager
{
    // Today's and upcoming bookings across all locations of the manager's restaurant
    public class ReservationsModel : ManagerPageModel
    {
        private readonly IStaffWorkService _work;
        private readonly IReservationService _reservations;

        public ReservationsModel(
            IStaffLocationService staffLocations,
            IRestaurantAdminService admin,
            IStaffWorkService work,
            IReservationService reservations)
            : base(staffLocations, admin)
        {
            _work = work;
            _reservations = reservations;
        }

        // Page number and size from the query string. Action redirects keep them.
        [BindProperty(SupportsGet = true)]
        public PageQuery Query { get; set; } = new();

        public PagedResult<Reservation> Bookings { get; private set; }

        protected override object ReturnRoute => new { Query.Page, Query.PageSize };

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            await LoadWorkspaceAsync(null, ct);
            if (Workspace is not null)
                Bookings = await _work.GetRestaurantReservationsPageAsync(Workspace.Restaurant.Id, _reservations.LocalNow().Date, Query.Page, Query.PageSize, ct);

            return Page();
        }

        public Task<IActionResult> OnPostSeatAsync(long reservationId, CancellationToken ct) =>
            RunAsync(ws => _reservations.SeatAsync(ws.Restaurant.Id, reservationId, ct), "Guests are seated.", ct);

        public Task<IActionResult> OnPostCompleteAsync(long reservationId, CancellationToken ct) =>
            RunAsync(ws => _reservations.CompleteAsync(ws.Restaurant.Id, reservationId, ct), "Booking completed.", ct);

        public Task<IActionResult> OnPostNoShowAsync(long reservationId, CancellationToken ct) =>
            RunAsync(ws => _reservations.MarkNoShowAsync(ws.Restaurant.Id, reservationId, ct), "Marked as no-show.", ct);
    }
}
