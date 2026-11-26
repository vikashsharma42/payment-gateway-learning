using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Web.Infrastructure;

namespace DineBook.Web.Pages.Restaurants
{
    // /{slug} opens the restaurant's default location, or its first open location
    public class IndexModel : AppPageModel
    {
        private readonly IReservationService _reservations;

        public IndexModel(IReservationService reservations)
        {
            _reservations = reservations;
        }

        [BindProperty(SupportsGet = true)]
        public string Slug { get; set; } = string.Empty;

        public Restaurant Restaurant { get; private set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            Restaurant = await _reservations.GetRestaurantBySlugAsync(Slug, ct);
            if (Restaurant is null || !Restaurant.IsActive)
                return NotFound();

            var location = Restaurant.ActiveLocations.FirstOrDefault(l => l.IsDefault)
                ?? Restaurant.ActiveLocations.FirstOrDefault();

            // No open location: show the restaurant with a notice
            if (location is null)
                return Page();

            return RedirectToPage("/Restaurants/Location", new { slug = Restaurant.Slug, locationSlug = location.Slug });
        }
    }
}
