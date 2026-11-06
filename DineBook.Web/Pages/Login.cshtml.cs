
using DineBook.Application.Services.Interface;
using DineBook.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages
{
    [EnableRateLimiting("checkout")]
    public class LoginModel : PageModel
    {
        private readonly IWebAuthenticationService _authentication;

        public LoginModel(IWebAuthenticationService authentication)
        {
            _authentication = authentication;
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

        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool Denied { get; set; }

        public string Error { get; private set; }

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                if (Enum.TryParse<UserRole>(roleClaim, out var role))
                    return LandingPageFor(role);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _authentication.LoginAsync(
                Email, Password, ct);

            if (user is null)
            {
                Error = "That email or password is not correct.";
                return Page();
            }

            if (!string.IsNullOrEmpty(ReturnUrl) &&
                Url.IsLocalUrl(ReturnUrl))
            {
                return LocalRedirect(ReturnUrl);
            }

            return LandingPageFor(user.Role);
        }

        private RedirectToPageResult LandingPageFor(UserRole role) =>
            role == UserRole.Admin
                ? RedirectToPage("/Admin/Index")
                : RedirectToPage("/Index");
    }
}