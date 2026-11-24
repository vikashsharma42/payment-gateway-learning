using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using DineBook.Web.Infrastructure;

namespace DineBook.Web.Pages.Orders
{
    [Authorize]
    public class IndexModel : AppPageModel
    {
        private readonly CustomerBookings _bookings;

        public IndexModel(CustomerBookings bookings)
        {
            _bookings = bookings;
        }

        // Only bookings that have food
        public IReadOnlyList<BookingWithOrders> History { get; private set; } = Array.Empty<BookingWithOrders>();

        public async Task OnGetAsync(CancellationToken ct)
        {
            var all = await _bookings.LoadAsync(UserId, ct);
            History = all.Where(b => b.FoodOrders.Count > 0).ToList();
        }
    }
}
