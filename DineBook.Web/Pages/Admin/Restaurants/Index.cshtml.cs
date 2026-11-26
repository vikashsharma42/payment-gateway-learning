using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Exceptions;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Admin.Restaurants
{
    // All restaurants: open one to manage it, or activate and deactivate it
    public class IndexModel : AppPageModel
    {
        private readonly IReservationService _reservations;
        private readonly IRestaurantAdminService _admin;

        public IndexModel(IReservationService reservations, IRestaurantAdminService admin)
        {
            _reservations = reservations;
            _admin = admin;
        }

        public IReadOnlyList<Restaurant> Restaurants { get; private set; } = Array.Empty<Restaurant>();

        public async Task OnGetAsync(CancellationToken ct)
        {
            Restaurants = await _reservations.GetRestaurantsAsync(ct);
        }

        public async Task<IActionResult> OnPostSetActiveAsync(long restaurantId, bool active, CancellationToken ct)
        {
            try
            {
                await _admin.SetRestaurantActiveAsync(restaurantId, active, ct);
                ToastSuccess(active
                    ? "Restaurant activated. Customers can book again."
                    : "Restaurant deactivated. Customers cannot book there.");
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage();
        }
    }
}
