using DineBook.Application.Services.Interface;
using DineBook.Domain.Exceptions;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages
{
    // Same per-IP limit as sign-in and checkout, so sign-ups cannot be spammed
    [EnableRateLimiting("checkout")]
    public class RegistrationModel : AppPageModel
    {
        private readonly IRegistrationService _registration;

        public RegistrationModel(IRegistrationService registration)
        {
            _registration = registration;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

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

        public void OnGet()
        {
        }

        // Live check while typing. GET, so it does not use up the sign-up rate limit.
        public async Task<JsonResult> OnGetIsEmailAvailable(string email, CancellationToken ct)
        {
            return new JsonResult(new { available = await _registration.IsEmailAvailableAsync(email, ct) });
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return Page();

            try
            {
                var session = await _registration.RegisterCustomerAsync(new CustomerRegistrationRequest(
                    Input.FirstName, Input.LastName, Input.Email, Input.Phone, Input.Password), ct);

                // Signed in straight away, like the sign-in page does
                AuthCookies.Set(Response, session.Tokens);
                ToastSuccess("Welcome to DineBook! Your account is ready.");
                return RedirectToPage("/Index");
            }
            catch (DomainException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }
}
