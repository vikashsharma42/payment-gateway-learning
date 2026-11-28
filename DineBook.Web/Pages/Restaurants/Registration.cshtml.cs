using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Exceptions;
using DineBook.Web.Dtos;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Restaurants
{
    // /{slug}/Registration: customers register with the restaurant whose address they opened.
    // Sign-up shares the sign-in limit per client address, so sign-ups cannot be spammed
    [EnableRateLimiting("auth")]
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

        // Restaurant web address from the URL
        [FromRoute]
        public string Slug { get; set; } = string.Empty;

        public Restaurant Restaurant { get; private set; }

        [BindProperty]
        public CustomerRegistrationDto Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            if (!await LoadRestaurantAsync(ct))
                return NotFound();

            // Already signed in: nothing to register, so go to the restaurant
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToPage("/Restaurants/Index", new { slug = Restaurant.Slug });

            return Page();
        }

        // Live check while typing. GET, so it does not use up the sign-up rate limit.
        public async Task<JsonResult> OnGetIsEmailAvailable(string email, CancellationToken ct)
        {
            if (!await LoadRestaurantAsync(ct))
                return new JsonResult(new { available = false });

            return new JsonResult(new { available = await _registration.IsEmailAvailableAsync(email, Restaurant.Id, ct) });
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!await LoadRestaurantAsync(ct))
                return NotFound();

            if (!ModelState.IsValid)
                return Page();

            try
            {
                // What was typed becomes the stored form (dial code + number) for the chosen country
                var phone = DineBook.Domain.ValueObjects.PhoneNumbers.Resolve(Input.CountryCode, Input.Phone);
                await _registration.RegisterCustomerAsync(new CustomerRegistrationRequest(
                    Input.FirstName, Input.LastName, Input.Email, Input.CountryCode, phone, Input.Password, Restaurant.Id), ct);

                // Signed in straight away, through the same sign-in as the login page
                await _authentication.LoginAsync(Input.Email, Input.Password, Restaurant.Id, ct);

                ToastSuccess("Welcome to DineBook! Your account is ready.");
                return RedirectToPage("/Restaurants/Index", new { slug = Restaurant.Slug });
            }
            catch (DomainException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }

        private async Task<bool> LoadRestaurantAsync(CancellationToken ct)
        {
            Restaurant = await _reservations.GetRestaurantBySlugAsync(Slug, ct);
            return Restaurant is not null && Restaurant.IsActive;
        }
    }
}
