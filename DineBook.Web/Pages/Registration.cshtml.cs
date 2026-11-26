using DineBook.Application.Services.Interface;
using DineBook.Domain.Exceptions;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages
{
    // Same per-IP limit as sign-in and checkout, so sign-ups cannot be spammed
    [EnableRateLimiting("checkout")]
    public class RegistrationModel : AppPageModel
    {
        private readonly IRegistrationService _registration;
        private readonly IReservationService _reservations;
        private readonly IWebAuthenticationService _authentication;

        public RegistrationModel(
            IRegistrationService registration,
            IReservationService reservations,
            IWebAuthenticationService authentication)
        {
            _registration = registration;
            _reservations = reservations;
            _authentication = authentication;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        // Restaurants a customer can register with
        public IEnumerable<SelectListItem> RestaurantOptions { get; private set; } = Enumerable.Empty<SelectListItem>();

        public class InputModel
        {
            [Required(ErrorMessage = "First name is required.")]
            [StringLength(100)]
            [Display(Name = "First name")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Last name is required.")]
            [StringLength(100)]
            [Display(Name = "Last name")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Enter a valid email.")]
            [StringLength(254)]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Phone is required.")]
            [RegularExpression(@"^\+?[0-9 ]{10,15}$", ErrorMessage = "Enter a valid phone number.")]
            [Display(Name = "Phone")]
            public string Phone { get; set; }

            [Required(ErrorMessage = "Choose the restaurant you want to book with.")]
            [Display(Name = "Restaurant")]
            public long? RestaurantId { get; set; }

            [Required(ErrorMessage = "Password is required.")]
            [StringLength(128, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters.")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Please confirm your password.")]
            [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(CancellationToken ct)
        {
            await LoadRestaurantsAsync(ct);
        }

        // Live check while typing. GET, so it does not use up the sign-up rate limit.
        public async Task<JsonResult> OnGetIsEmailAvailable(string email, long? restaurantId, CancellationToken ct)
        {
            return new JsonResult(new { available = await _registration.IsEmailAvailableAsync(email, restaurantId, ct) });
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadRestaurantsAsync(ct);
                return Page();
            }

            try
            {
                await _registration.RegisterCustomerAsync(new CustomerRegistrationRequest(
                    Input.FirstName, Input.LastName, Input.Email, Input.Phone, Input.Password, Input.RestaurantId.Value), ct);

                // Signed in straight away, through the same sign-in as the login page
                await _authentication.LoginAsync(Input.Email, Input.Password, Input.RestaurantId, ct);

                ToastSuccess("Welcome to DineBook! Your account is ready.");

                // Straight to the restaurant the customer registered with
                var restaurant = await _reservations.GetRestaurantAsync(Input.RestaurantId.Value, ct);
                return restaurant is null
                    ? RedirectToPage("/Login")
                    : RedirectToPage("/Restaurants/Index", new { slug = restaurant.Slug });
            }
            catch (DomainException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadRestaurantsAsync(ct);
                return Page();
            }
        }

        private async Task LoadRestaurantsAsync(CancellationToken ct)
        {
            var restaurants = await _reservations.GetRestaurantsAsync(ct);
            RestaurantOptions = restaurants
                .Where(r => r.IsActive)
                .Select(r => new SelectListItem(r.Name, r.Id.ToString(CultureInfo.CurrentCulture)))
                .ToList();
        }
    }
}
