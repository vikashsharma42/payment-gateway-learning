using DineBook.Application.Security;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages
{
    // Two sign-in pages share this model:
    //   /{slug}/Login  customers, staff and managers of that restaurant
    //   /Login         administrators (no restaurant)
    [EnableRateLimiting("auth")]
    public class LoginModel : AppPageModel
    {
        private readonly IWebAuthenticationService _authentication;
        private readonly IReservationService _reservations;
        private readonly ISecurityService _security;

        public LoginModel(IWebAuthenticationService authentication, IReservationService reservations, ISecurityService security)
        {
            _authentication = authentication;
            _reservations = reservations;
            _security = security;
        }

        // Restaurant web address from the URL. Empty on /Login.
        [FromRoute]
        public string Slug { get; set; }

        // The shared sign-in fields (email and password) and their rules
        [BindProperty]
        public CredentialsInput Input { get; set; }

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

            // Already signed in: skip the form and go where this account belongs
            if (User.Identity?.IsAuthenticated == true && Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var role))
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

            var email = Input.Email.Trim().ToLowerInvariant();
            var address = ClientInfo.Address(HttpContext);
            var agent = ClientInfo.Agent(HttpContext);
            var path = Request.Path.Value ?? string.Empty;

            // Brute-force protection: repeated failures for this account or address pause sign-in for a while
            if (await _security.IsSignInLockedAsync(email, address, ct))
            {
                await _security.RecordAsync(SecurityEventKind.LoginLocked, address, agent, Request.Method, path, email,
                    "Sign-in paused after repeated failures", true, ct);
                Error = "Too many unsuccessful sign-in attempts. Please wait 15 minutes and try again.";
                return Page();
            }

            // Administrators are looked up platform-wide (no restaurant). Everyone else must use their restaurant's address.
            var user = await _authentication.LoginAsync(Input.Email.Trim(), Input.Password, Restaurant?.Id, ct);

            if (user is null)
            {
                await _security.RecordAsync(SecurityEventKind.LoginFailed, address, agent, Request.Method, path, email,
                    "Wrong email or password", false, ct);
                Error = Restaurant is null
                    ? "That email or password is not correct for an administrator account."
                    : "That email or password is not correct for this restaurant.";
                return Page();
            }

            ToastSuccess($"Welcome back, {user.FirstName}.");
            return await AfterSignInAsync(user.Role, user.RestaurantId, ct) ?? Page();
        }

        // Null means there is nowhere to go, so the sign-in page stays.
        private async Task<IActionResult> AfterSignInAsync(UserRole role, long? restaurantId, CancellationToken ct)
        {
            // Customers go to the restaurant whose address they used, or else to their own restaurant
            var slug = Restaurant?.Slug;
            if (slug is null && role == UserRole.Customer && restaurantId is { } id)
                slug = (await _reservations.GetRestaurantAsync(id, ct))?.Slug;

            return RedirectAfterSignIn(role, slug, ReturnUrl);
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
