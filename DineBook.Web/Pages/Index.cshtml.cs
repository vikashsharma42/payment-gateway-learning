using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;

namespace DineBook.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IReservationService _reservations;

        public IndexModel(IReservationService reservations)
        {
            _reservations = reservations;
        }

        public IReadOnlyList<Restaurant> Restaurants { get; private set; } = Array.Empty<Restaurant>();

        public async Task OnGetAsync(CancellationToken ct)
        {
            Restaurants = await _reservations.GetRestaurantsAsync(ct);

            // The header links for My bookings, Orders and sign-in use this restaurant's address
            ViewData["NavSlug"] = Restaurants.Count > 0 ? Restaurants[0].Slug : null;
        }
    }
}
