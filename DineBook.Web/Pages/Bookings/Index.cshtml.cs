using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using DineBook.Domain.Enums;
using DineBook.Web.Infrastructure;

namespace DineBook.Web.Pages.Bookings
{
    [Authorize]
    public class IndexModel : AppPageModel
    {
        private readonly CustomerBookings _bookings;

        public IndexModel(CustomerBookings bookings)
        {
            _bookings = bookings;
        }

        public IReadOnlyList<BookingWithOrders> Bookings { get; private set; } = Array.Empty<BookingWithOrders>();

        public async Task OnGetAsync(CancellationToken ct)
        {
            Bookings = await _bookings.LoadAsync(UserId, ct);
        }

        public static string BadgeFor(ReservationStatus status) => status switch
        {
            ReservationStatus.Confirmed => "text-bg-success",
            ReservationStatus.Seated => "text-bg-primary",
            ReservationStatus.Completed => "text-bg-secondary",
            _ => "text-bg-danger"
        };
    }
}
