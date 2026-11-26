
using DineBook.Application.Services.Interface;
using DineBook.Domain.Enums;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages
{
    [EnableRateLimiting("checkout")]
    public class LoginModel : AppPageModel
    {
        private readonly IWebAuthenticationService _authentication;
        private readonly IReservationService _reservations;

        public LoginModel(IWebAuthenticationService authentication, IReservationService reservations)
        {
            _authentication = authentication;
            _reservations = reservations;
        }

        [BindProperty]
        [Required(ErrorMessage = "Enter your email.")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Enter your password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        // Customers and staff sign in within their restaurant. Administrators leave it empty.
        [BindProperty]
        [Display(Name = "Restaurant")]
        public long? RestaurantId { get; set; }

        // Restaurant web address (?slug=...), used to pre-select the restaurant, e.g. from the booking page
        [BindProperty(SupportsGet = true, Name = "slug")]
        public string Slug { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool Denied { get; set; }

        public string Error { get; private set; }

        public IEnumerable<SelectListItem> RestaurantOptions { get; private set; } = Enumerable.Empty<SelectListItem>();

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                // Customers have no home page, so a signed-in customer simply sees this page
                if (Enum.TryParse<UserRole>(roleClaim, out var role) && role != UserRole.Customer)
                    return LandingPageFor(role);
            }

            await LoadRestaurantsAsync(ct);

            if (!string.IsNullOrWhiteSpace(Slug))
            {
                var restaurant = await _reservations.GetRestaurantBySlugAsync(Slug, ct);
                RestaurantId = restaurant?.Id;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadRestaurantsAsync(ct);
                return Page();
            }

            var user = await _authentication.LoginAsync(Email, Password, RestaurantId, ct);

            if (user is null)
            {
                await LoadRestaurantsAsync(ct);
                Error = "That email or password is not correct for the chosen restaurant.";
                return Page();
            }

            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                return LocalRedirect(ReturnUrl);

            // Customers book from their restaurant's own link (/{slug}), so they stay on this page with a hint
            if (user.Role == UserRole.Customer)
                ToastSuccess("You are signed in. Open a restaurant's link to book a table.");

            return LandingPageFor(user.Role);
        }

        // Admins go to the admin list, restaurant staff to their location page, customers back to this page
        private RedirectToPageResult LandingPageFor(UserRole role) => role switch
        {
            UserRole.Admin => RedirectToPage("/Admin/Dashboard"),
            UserRole.Staff or UserRole.Manager => RedirectToPage("/Staff/Dashboard"),
            _ => RedirectToPage("/Login")
        };

        private async Task LoadRestaurantsAsync(CancellationToken ct)
        {
            var restaurants = await _reservations.GetRestaurantsAsync(ct);
            RestaurantOptions = restaurants
                .Where(r => r.IsActive)
                .Select(r => new SelectListItem(r.Name, r.Id.ToString(CultureInfo.InvariantCulture)))
                .ToList();
        }
    }
}
