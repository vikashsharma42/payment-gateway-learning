using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

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

        // Location used by the "Book a table" and "View menu" buttons when there are no orders yet
        public Location DefaultLocation { get; private set; }

        public IReadOnlyList<Orders> Orders { get; private set; } = Array.Empty<Orders>();

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            Restaurant = await _reservations.GetRestaurantBySlugAsync(Slug, ct);
            if (Restaurant is null || !Restaurant.IsActive)
                return NotFound();

            // Only customers of this restaurant have its orders. Everyone else signs in here first.
            if (!IsCustomerOf(Restaurant.Id))
                return RedirectToPage("/Restaurants/Login", new { slug = Slug, ReturnUrl = Request.Path.ToString() });

            DefaultLocation = Restaurant.ActiveLocations.FirstOrDefault(l => l.IsDefault)
                ?? Restaurant.ActiveLocations.FirstOrDefault();

            Orders = await _customers.GetRestaurantOrdersAsync(UserId, Restaurant.Id, ct);
            return Page();
        }
    }
}