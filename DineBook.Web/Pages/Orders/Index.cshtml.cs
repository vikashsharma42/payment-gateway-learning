using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DineBook.Web.Infrastructure;

namespace DineBook.Web.Pages.Orders
{
    public class IndexModel : PageModel
    {
        private readonly BookingAccess _access;

        public IndexModel(BookingAccess access)
        {
            _access = access;
        }

        // Only bookings that have food
        public IReadOnlyList<BookingWithOrders> History { get; private set; } = Array.Empty<BookingWithOrders>();

        public async Task OnGetAsync(CancellationToken ct)
        {
            var all = await _access.LoadMyBookingsAsync(ct);
            History = all.Where(b => b.FoodOrders.Count > 0).ToList();
        }
    }
}
