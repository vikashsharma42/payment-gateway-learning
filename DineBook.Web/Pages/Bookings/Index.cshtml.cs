using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DineBook.Domain.Enums;
using DineBook.Web.Infrastructure;

namespace DineBook.Web.Pages.Bookings
{
    public class IndexModel : PageModel
    {
        private readonly BookingAccess _access;

        public IndexModel(BookingAccess access)
        {
            _access = access;
        }

        public IReadOnlyList<BookingWithOrders> Bookings { get; private set; } = Array.Empty<BookingWithOrders>();

        public bool IsSignedIn => _access.IsSignedIn;

        public async Task OnGetAsync(CancellationToken ct)
        {
            Bookings = await _access.LoadMyBookingsAsync(ct);
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
