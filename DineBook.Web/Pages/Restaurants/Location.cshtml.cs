using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Web.Infrastructure;

namespace DineBook.Web.Pages.Restaurants
{
    // /restaurants/{slug}/{locationSlug}: details of one open location of a restaurant
    public class LocationModel : AppPageModel
    {
        private readonly IReservationService _reservations;

        public LocationModel(IReservationService reservations)
        {
            _reservations = reservations;
        }

        [BindProperty(SupportsGet = true)]
        public string Slug { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string LocationSlug { get; set; } = string.Empty;

        public Restaurant Restaurant { get; private set; }

        public Location SelectedLocation { get; private set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            Restaurant = await _reservations.GetRestaurantBySlugAsync(Slug, ct);
            if (Restaurant is null || !Restaurant.IsActive)
                return NotFound();

            SelectedLocation = Restaurant.ActiveLocations
                .FirstOrDefault(l => string.Equals(l.Slug, LocationSlug, StringComparison.OrdinalIgnoreCase));
            if (SelectedLocation is null)
                return NotFound();

            return Page();
        }
    }
}
