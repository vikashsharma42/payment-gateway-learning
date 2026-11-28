using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;
using DineBook.Application.Security;
using DineBook.Domain.Enums;
using DineBook.Domain.ValueObjects;

namespace DineBook.Web.Infrastructure
{
    public abstract class AppPageModel : PageModel
    {
        private const string ToastKey = "Toast";

        // The signed-in account's id (set from the cookie at sign-in). 0 when nobody is signed in.
        protected long UserId =>
            long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        // True only when the signed-in account is a customer registered with this restaurant.
        // The sign-in cookie covers the whole site, so being signed in elsewhere must not open this restaurant's pages.
        protected bool IsCustomerOf(long restaurantId) =>
            User.Identity?.IsAuthenticated == true
            && User.FindFirstValue(ClaimTypes.Role) == nameof(UserRole.Customer)
            && UserClaimsFactory.RestaurantIdOf(User) == restaurantId;

        protected void ToastSuccess(string message) => SetToast("success", message);

        protected void ToastError(string message) => SetToast("danger", message);

        private void SetToast(string type, string message) =>
            TempData[ToastKey] = JsonSerializer.Serialize(new ToastMessage(type, message));

        // Where a signed-in user goes next. Every sign-in page uses this one rule.
        // A safe ReturnUrl wins. Admins go to their dashboard, staff and managers to the staff dashboard,
        // and customers to their restaurant. Null means there is nowhere to go, so the caller keeps the sign-in page.
        protected IActionResult RedirectAfterSignIn(UserRole role, string restaurantSlug, string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            switch (role)
            {
                case UserRole.Admin:
                    return RedirectToPage("/Admin/Dashboard");
                case UserRole.Staff:
                case UserRole.Manager:
                    return RedirectToPage("/Staff/Dashboard");
                default:
                    return string.IsNullOrEmpty(restaurantSlug)
                        ? null
                        : RedirectToPage("/Restaurants/Index", new { slug = restaurantSlug });
            }
        }

        // A page under /{slug}/... remembers that restaurant, so the header can link to it elsewhere.
        // Pages that answer "not found" do not count.
        public override void OnPageHandlerExecuted(PageHandlerExecutedContext context)
        {
            base.OnPageHandlerExecuted(context);

            if (context.Result is NotFoundResult)
                return;

            if (context.RouteData.Values.TryGetValue("slug", out var value)
                && value is string slug
                && RestaurantSlug.IsValid(slug))
            {
                RestaurantCookie.Remember(Response, slug);
            }
        }
    }

    // Read by _Layout.cshtml and shown by app.js
    public sealed record ToastMessage(string Type, string Message);
}
