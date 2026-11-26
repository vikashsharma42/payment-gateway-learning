using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DineBook.Web.Pages
{
    // "/" lists restaurants that have at least one open location. Browsing needs no sign-in.
    public class DirectoryModel : AppPageModel
    {
        //private readonly IReservationService _reservations;

        //public DirectoryModel(IReservationService reservations)
        //{
        //    _reservations = reservations;
        //}

        //public IReadOnlyList<Restaurant> Restaurants { get; private set; } = new List<Restaurant>();

        //public async Task OnGetAsync(CancellationToken ct)
        //{
        //    Restaurants = (await _reservations.GetRestaurantsAsync(ct))
        //        .Where(r => r.IsActive && r.ActiveLocations.Any())
        //        .OrderBy(r => r.Name)
        //        .ToList();
        //}

        public IActionResult OnGetAsync()
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return Page();
        }
    }
}