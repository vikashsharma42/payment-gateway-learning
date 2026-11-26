using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using DineBook.Application.Services.Interface;

namespace DineBook.Web.Pages.Admin
{
    // Shared sign-out for every signed-in user (header button and admin page)
    public class LogoutModel : PageModel
    {
        private readonly IWebAuthenticationService _authentication;

        public LogoutModel(IWebAuthenticationService authentication)
        {
            _authentication = authentication;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await _authentication.LogoutAsync();

            // The chosen staff location belongs to the session that is ending
            Response.Cookies.Delete(DineBook.Web.Pages.Staff.IndexModel.LocationCookie);

            return RedirectToPage("/Index");
        }
    }
}
