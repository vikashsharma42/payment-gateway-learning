using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DineBook.Application.Services.Interface;
using DineBook.Web.Infrastructure;

namespace DineBook.Web.Pages.Admin
{
    public class LogoutModel : PageModel
    {
        private readonly IAccountService _accounts;

        public LogoutModel(IAccountService accounts)
        {
            _accounts = accounts;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Only a signed-in user has sessions to end. The cookies are cleared either way.
            if (User.Identity?.IsAuthenticated == true)
            {
                var refreshToken = Request.Cookies[AuthCookies.RefreshName] ?? string.Empty;
                await _accounts.LogoutAsync(User.FindFirstValue(JwtRegisteredClaimNames.Jti), refreshToken);
            }

            AuthCookies.Clear(Response);
            return RedirectToPage("/Index");
        }
    }
}
