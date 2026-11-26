using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Web.Infrastructure;

namespace DineBook.Web.Pages.Restaurants
{
    // /{slug}/{locationSlug}/menu: the dishes available at the restaurant
    public class MenuModel : AppPageModel
    {
        private readonly IReservationService _reservations;
        private readonly IMenuService _menu;

        public MenuModel(IReservationService reservations, IMenuService menu)
        {
            _reservations = reservations;
            _menu = menu;
        }

        [BindProperty(SupportsGet = true)]
        public string Slug { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string LocationSlug { get; set; } = string.Empty;

        public Restaurant Restaurant { get; private set; }

        public Location SelectedLocation { get; private set; }

        public IReadOnlyList<MenuItem> Menu { get; private set; } = Array.Empty<MenuItem>();

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            Restaurant = await _reservations.GetRestaurantBySlugAsync(Slug, ct);
            if (Restaurant is null || !Restaurant.IsActive)
                return NotFound();

            SelectedLocation = Restaurant.ActiveLocations
                .FirstOrDefault(l => string.Equals(l.Slug, LocationSlug, StringComparison.OrdinalIgnoreCase));
            if (SelectedLocation is null)
                return NotFound();

            Menu = await _menu.GetAvailableItemsAsync(Restaurant.Id, ct);
            return Page();
        }
    }
}
