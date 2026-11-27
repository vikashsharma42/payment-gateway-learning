using DineBook.Application.Services.Interface;
using DineBook.Domain.Enums;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DineBook.Web.Pages
{
    // The one sign-out for every role (admin, staff, manager, customer). Anyone may open it;
    // signing out when not signed in does nothing harmful.
    public class LogoutModel : AppPageModel
    {
        private readonly IWebAuthenticationService _authentication;

        public LogoutModel(IWebAuthenticationService authentication)
        {
            _authentication = authentication;
        }

        public bool IsSignedIn => User.Identity?.IsAuthenticated == true;

        // Where "Stay signed in" goes: the dashboard for the role, or the restaurant for customers
        public string CancelPage { get; private set; }

        public string CancelSlug { get; private set; }

        public void OnGet()
        {
            if (!Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var role))
                return;

            switch (role)
            {
                case UserRole.Admin:
                    CancelPage = "/Admin/Dashboard";
                    break;
                case UserRole.Staff:
                case UserRole.Manager:
                    CancelPage = "/Staff/Dashboard";
                    break;
                default:
                    CancelSlug = RestaurantCookie.Recall(Request);
                    if (CancelSlug is not null)
                        CancelPage = "/Restaurants/Index";
                    break;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Read before signing out: a customer returns to the restaurant they were using
            var isCustomer = User.FindFirstValue(ClaimTypes.Role) == nameof(UserRole.Customer);
            var slug = RestaurantCookie.Recall(Request);

            await _authentication.LogoutAsync();

            // The chosen staff location belongs to the session that is ending
            Response.Cookies.Delete(DineBook.Web.Pages.Staff.IndexModel.LocationCookie);

            ToastSuccess("You have signed out.");

            if (isCustomer && slug is not null)
                return RedirectToPage("/Restaurants/Index", new { slug });

            return RedirectToPage("/Login");
        }
    }
}
