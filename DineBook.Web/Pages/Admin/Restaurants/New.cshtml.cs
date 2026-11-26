using DineBook.Application.Services.Interface;
using DineBook.Domain.Exceptions;
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
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Location name is required.")]
        [StringLength(100)]
        public string LocationName { get; set; } = "Main";

        [BindProperty]
        [Required(ErrorMessage = "Street is required.")]
        [StringLength(200)]
        public string Street { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "City is required.")]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "State is required.")]
        [StringLength(100)]
        public string State { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Country is required.")]
        [StringLength(100)]
        public string Country { get; set; } = "India";

        [BindProperty]
        [Required(ErrorMessage = "Zip code is required.")]
        [StringLength(20)]
        public string ZipCode { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Opening time is required.")]
        [DataType(DataType.Time)]
        public TimeOnly OpensAt { get; set; } = new TimeOnly(12, 0);

        [BindProperty]
        [Required(ErrorMessage = "Closing time is required.")]
        [DataType(DataType.Time)]
        public TimeOnly ClosesAt { get; set; } = new TimeOnly(23, 0);

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
                    Name, OpensAt, ClosesAt, LocationName, Street, City, State, Country, ZipCode, Phone: null), ct);

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
