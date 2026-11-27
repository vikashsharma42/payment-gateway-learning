using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DineBook.Web.Pages.Admin
{
    // Kept so old links and forms still work. Sign-out for every role is the shared /Logout page.
    // A POST keeps its method on the redirect (308), so the sign-out really runs. Any role may use it,
    // because the folder rule would otherwise block staff and managers with an old form.
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet() => RedirectToPage("/Logout");

        public IActionResult OnPost() => RedirectToPagePreserveMethod("/Logout");
    }
}
