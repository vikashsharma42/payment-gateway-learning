using DineBook.Application.Services.Interface;
using DineBook.Domain.Exceptions;
using DineBook.Web.Dtos;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Admin.Restaurants
{
    public class NewModel : AppPageModel
    {
        private readonly IRestaurantAdminService _admin;

        public NewModel(IRestaurantAdminService admin)
        {
            _admin = admin;
        }

        [BindProperty]
        public RestaurantCreateDto Input { get; set; } = new();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return Page();

            try
            {
                var restaurant = await _admin.CreateRestaurantAsync(new CreateRestaurantRequest(
                    Input.Name, Input.OpensAt, Input.ClosesAt, Input.LocationName, Input.Street, Input.City,
                    Input.State, Input.Country, Input.ZipCode, Phone: null), ct);

                ToastSuccess("Restaurant created. Now add its tables and menu.");
                return RedirectToPage("/Admin/Restaurants/Manage", new { id = restaurant.Id });
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
                return Page();
            }
        }
    }
}
