using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;
using DineBook.Web.Dtos;
using DineBook.Web.Infrastructure;

namespace DineBook.Web.Pages.Restaurants
{
    // /{slug}/Profile: the signed-in customer's own details. The restaurant in the address is where they are signed in.
    public class ProfileModel : AppPageModel
    {
        private readonly ICustomerService _customers;
        private readonly IReservationService _reservations;

        public ProfileModel(ICustomerService customers, IReservationService reservations)
        {
            _customers = customers;
            _reservations = reservations;
        }

        // Restaurant web address from the URL
        [FromRoute]
        public string Slug { get; set; } = string.Empty;

        public Restaurant Restaurant { get; private set; }

        [BindProperty]
        public CustomerProfileDto Input { get; set; } = new();

        // Shown but not editable here. Changing the sign-in email is not part of this version.
        public string Email { get; private set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            var denied = await EnsureCustomerAsync(ct);
            if (denied is not null)
                return denied;

            var profile = await _customers.GetProfileAsync(UserId, ct);
            Email = profile.Email;
            Input = new CustomerProfileDto
            {
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Phone = profile.Phone ?? string.Empty
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            var denied = await EnsureCustomerAsync(ct);
            if (denied is not null)
                return denied;

            if (!ModelState.IsValid)
            {
                await LoadEmailAsync(ct);
                return Page();
            }

            try
            {
                await _customers.UpdateProfileAsync(UserId, Input.FirstName, Input.LastName, Input.Phone, ct);
                ToastSuccess("Your profile has been updated.");
                return RedirectToPage(new { slug = Slug });
            }
            catch (DomainException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadEmailAsync(ct);
                return Page();
            }
        }

        // Loads the restaurant and checks the visitor is a signed-in customer. Returns a result to send back, or null to continue.
        private async Task<IActionResult> EnsureCustomerAsync(CancellationToken ct)
        {
            Restaurant = await _reservations.GetRestaurantBySlugAsync(Slug, ct);
            if (Restaurant is null || !Restaurant.IsActive)
                return NotFound();

            // Signed-out visitors sign in first, then come back here
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToPage("/Restaurants/Login", new { slug = Slug, ReturnUrl = Request.Path.ToString() });

            // Only customers have a profile here. Other accounts go to their own area.
            if (User.FindFirstValue(ClaimTypes.Role) != nameof(UserRole.Customer))
                return RedirectToPage("/Restaurants/Login", new { slug = Slug });

            return null;
        }

        private async Task LoadEmailAsync(CancellationToken ct)
        {
            Email = (await _customers.GetProfileAsync(UserId, ct)).Email;
        }
    }
}
