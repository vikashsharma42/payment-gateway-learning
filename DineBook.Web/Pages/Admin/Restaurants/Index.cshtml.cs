using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Web.Infrastructure;

namespace DineBook.Web.Pages
{
    // "/" lists restaurants that have at least one open location. Browsing needs no sign-in.
    public class IndexModel : AppPageModel
    {
        private readonly IReservationService _reservations;

        public IndexModel(IReservationService reservations)
        {
            _reservations = reservations;
        }

        public IReadOnlyList<Restaurant> Restaurants { get; private set; } = new List<Restaurant>();

        public async Task OnGetAsync(CancellationToken ct)
        {
            Restaurants = (await _reservations.GetRestaurantsAsync(ct))
                .Where(r => r.IsActive && r.ActiveLocations.Any())
                .OrderBy(r => r.Name)
                .ToList();
        }
    }
}
