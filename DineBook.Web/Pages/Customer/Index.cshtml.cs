using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DineBook.Web.Pages.Customer
{
    // /customer: home of the customer area. The customer area is limited to customers, so
    // signed-out visitors are sent to sign in first. Then they land on the dashboard.
    public class IndexModel : PageModel
    {
        public IActionResult OnGet() => RedirectToPage("/Customer/Dashboard");
    }
}
