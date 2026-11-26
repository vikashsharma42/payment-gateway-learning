using DineBook.Application.Security;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages
{
    // Two sign-in pages share this model:
    //   /{slug}/Login  customers, staff and managers of that restaurant
    //   /Login         administrators (no restaurant)
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

        // Restaurant web address from the URL. Empty on /Login.
        [FromRoute]
        public string Slug { get; set; }

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

        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool Denied { get; set; }

        // The restaurant named in the URL. Null on /Login.
        public Restaurant Restaurant { get; private set; }

        public string Error { get; private set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            if (!await LoadRestaurantAsync(ct))
                return NotFound();

            if (User.Identity?.IsAuthenticated == true
                && Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var role))
            {
                return await AfterSignInAsync(role, UserClaimsFactory.RestaurantIdOf(User), ct) ?? Page();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!await LoadRestaurantAsync(ct))
                return NotFound();

            if (!ModelState.IsValid)
                return Page();

            // Administrators are looked up platform-wide (no restaurant). Everyone else must use their restaurant's address.
            var user = await _authentication.LoginAsync(Email, Password, Restaurant?.Id, ct);

            if (user is null)
            {
                Error = Restaurant is null
                    ? "That email or password is not correct for an administrator account."
                    : "That email or password is not correct for this restaurant.";
                return Page();
            }

            return await AfterSignInAsync(user.Role, user.RestaurantId, ct) ?? Page();
        }

        // Where a signed-in user goes next. Null means there is nowhere to go, so the sign-in page stays.
        private async Task<IActionResult> AfterSignInAsync(UserRole role, long? restaurantId, CancellationToken ct)
        {
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                return LocalRedirect(ReturnUrl);

            switch (role)
            {
                case UserRole.Admin:
                    return RedirectToPage("/Admin/Dashboard");
                case UserRole.Staff:
                case UserRole.Manager:
                    return RedirectToPage("/Staff/Dashboard");
            }

            // Customers go to the restaurant whose address they used, or else to their own restaurant
            var target = Restaurant
                ?? (restaurantId is { } id ? await _reservations.GetRestaurantAsync(id, ct) : null);

            return target is null
                ? null
                : RedirectToPage("/Restaurants/Index", new { slug = target.Slug });
        }

        private async Task<bool> LoadRestaurantAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(Slug))
                return true;

            Restaurant = await _reservations.GetRestaurantBySlugAsync(Slug, ct);
            return Restaurant is not null && Restaurant.IsActive;
        }
    }
}
