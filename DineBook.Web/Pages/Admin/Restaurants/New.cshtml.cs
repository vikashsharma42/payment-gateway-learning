using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Exceptions;
using DineBook.Web.Infrastructure;

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
        [Required(ErrorMessage = "Pincode is required.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode must be 6 digits.")]
        public string Pincode { get; set; } = string.Empty;

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
                    Name, Street, City, State, Pincode, OpensAt, ClosesAt), ct);

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
