using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DineBook.Web.Pages.Admin
{
    // Kept so old links and the JWT "not signed in" redirect still work. The sign-in page is shared.
    public class LoginModel : PageModel
    {
        public IActionResult OnGet(string returnUrl, bool denied = false) =>
            RedirectToPage("/Login", new { returnUrl, denied });
    }
}
