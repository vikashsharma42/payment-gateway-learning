using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Web.Infrastructure;

namespace DineBook.Web.Pages.Admin
{
    public class IndexModel : AppPageModel
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
        }
    }
}
