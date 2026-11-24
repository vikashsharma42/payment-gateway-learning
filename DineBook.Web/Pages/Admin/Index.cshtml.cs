using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Web.Infrastructure;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Admin
{
    public class IndexModel : AppPageModel
    {
        private readonly IReservationService _reservations;

        public IndexModel(IReservationService reservations)
        {
            _reservations = reservations;
        }

        public IReadOnlyList<Restaurant> Restaurants { get; private set; }

        public async Task OnGetAsync(CancellationToken ct)
        {
            Restaurants = await _reservations.GetRestaurantsAsync(ct);
        }
    }
}
