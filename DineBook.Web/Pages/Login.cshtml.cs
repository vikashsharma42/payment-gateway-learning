using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Enums;
using DineBook.Web.Infrastructure;

namespace DineBook.Web.Pages
{
    // One sign-in page for every user. The role decides where they land and what they can open.
    // Brute-force protection: the same per-IP limit as checkout
    [EnableRateLimiting("checkout")]
    public class LoginModel : PageModel
    {
        private readonly IAccountService _accounts;

        public LoginModel(IAccountService accounts)
        {
            _accounts = accounts;
        }

        [BindProperty]
        [Required(ErrorMessage = "Enter your email.")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Enter your password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        // Where to go after signing in. Only local addresses are followed.
        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; } = string.Empty;

        // Set when a signed-in user without the needed role was sent here
        [BindProperty(SupportsGet = true)]
        public bool Denied { get; set; }

        public string Error { get; private set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return Page();

            var session = await _accounts.LoginAsync(Email, Password, ct);
            if (session is null)
            {
                // Same message for an unknown email, a wrong password and a deactivated account
                Error = "That email or password is not correct.";
                return Page();
            }

            AuthCookies.Set(Response, session.Tokens);

            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                return LocalRedirect(ReturnUrl);

            return LandingPageFor(session.Role);
        }

        // Where each role starts. Access rules are enforced by the page folders, not here.
        private IActionResult LandingPageFor(UserRole role) =>
            role == UserRole.Admin
                ? RedirectToPage("/Admin/Index")
                : RedirectToPage("/Index");
    }
}
