using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Enums;
using DineBook.Web.Infrastructure;

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
            // Read before signing out: a customer returns to the restaurant they were using
            var isCustomer = User.FindFirstValue(ClaimTypes.Role) == nameof(UserRole.Customer);
            var slug = RestaurantCookie.Recall(Request);

            await _authentication.LogoutAsync();

            // The chosen staff location belongs to the session that is ending
            Response.Cookies.Delete(DineBook.Web.Pages.Staff.IndexModel.LocationCookie);

            if (isCustomer && slug is not null)
                return RedirectToPage("/Restaurants/Index", new { slug });

            return RedirectToPage("/Login");
        }
    }
}
