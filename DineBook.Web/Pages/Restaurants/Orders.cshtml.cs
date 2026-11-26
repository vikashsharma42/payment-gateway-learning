using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Web.Infrastructure;

namespace DineBook.Web.Pages.Restaurants
{
    // /{slug}/Orders: the signed-in customer's food orders at this restaurant
    public class OrdersModel : AppPageModel
    {
        private readonly ICustomerService _customers;
        private readonly IReservationService _reservations;

        public OrdersModel(ICustomerService customers, IReservationService reservations)
        {
            _customers = customers;
            _reservations = reservations;
        }

        // Restaurant web address from the URL
        [FromRoute]
        public string Slug { get; set; } = string.Empty;

        public Restaurant Restaurant { get; private set; }

        public IReadOnlyList<Orders> Orders { get; private set; } = Array.Empty<Orders>();

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            Restaurant = await _reservations.GetRestaurantBySlugAsync(Slug, ct);
            if (Restaurant is null || !Restaurant.IsActive)
                return NotFound();

            // Signed-out visitors sign in first, then come back here
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToPage("/Restaurants/Login", new { slug = Slug, ReturnUrl = Request.Path.ToString() });

            // Only customers have orders. Other accounts go to their own area.
            if (User.FindFirstValue(ClaimTypes.Role) != nameof(UserRole.Customer))
                return RedirectToPage("/Restaurants/Login", new { slug = Slug });

            Orders = await _customers.GetRestaurantOrdersAsync(UserId, Restaurant.Id, ct);
            return Page();
        }
    }
}
