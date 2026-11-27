using DineBook.Application.Paging;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;
using DineBook.Web.Dtos;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Admin.Users
{
    public class IndexModel : AppPageModel
    {
        private readonly IRegistrationService _registration;
        private readonly IReservationService _reservations;

        public IndexModel(IRegistrationService registration, IReservationService reservations)
        {
            _registration = registration;
            _reservations = reservations;
        }

        // Search by email, name or phone (?Search=...)
        public UserSearchQuery Query { get; private set; } = new();

        public PagedResult<ApplicationUser> Users { get; private set; }

        // Restaurants to choose from, and their names for the list
        public IReadOnlyList<Restaurant> Restaurants { get; private set; } = new List<Restaurant>();

        public IEnumerable<SelectListItem> RestaurantOptions =>
            Restaurants.Select(r => new SelectListItem(r.Name, r.Id.ToString(CultureInfo.InvariantCulture)));

        public string RestaurantNameFor(ApplicationUser user) =>
            user.RestaurantId is null
                ? string.Empty
                : Restaurants.FirstOrDefault(r => r.Id == user.RestaurantId)?.Name ?? "(removed)";

        [BindProperty]
        public StaffUserCreateDto Input { get; set; } = new();

        // Set when the create popup has to be shown again with its errors
        public bool ShowCreateModal { get; private set; }

        public IEnumerable<SelectListItem> RoleOptions { get; } =
            new[] { UserRole.Staff, UserRole.Manager, UserRole.Admin }
                .Select(r => new SelectListItem(r.ToString(), r.ToString()));

        public async Task OnGetAsync([FromQuery] UserSearchQuery query, CancellationToken ct)
        {
            Query = query;
            await LoadAsync(ct);
        }

        // Live check while typing in the popup (GET, same as the sign-up page)
        public async Task<JsonResult> OnGetIsEmailAvailable(string email, long? restaurantId, CancellationToken ct)
        {
            return new JsonResult(new { available = await _registration.IsEmailAvailableAsync(email, restaurantId, ct) });
        }

        public async Task<IActionResult> OnPostCreateAsync([FromQuery] UserSearchQuery query, CancellationToken ct)
        {
            // Keep the search and page the admin was on, for the redirect and the redisplay
            Query = query;

            if (!ModelState.IsValid)
            {
                await LoadAsync(ct);
                ShowCreateModal = true;
                return Page();
            }

            try
            {
                await _registration.CreateStaffUserAsync(new StaffUserRequest(
                    Input.FirstName, Input.LastName, Input.Email, Input.Password, Input.Role, Input.RestaurantId), ct);

                ToastSuccess($"{Input.FirstName} {Input.LastName} was added as {Input.Role}.");
                return RedirectToPage(new { Search = Query.Search, Page = Query.Page, PageSize = Query.PageSize });
            }
            catch (DomainException ex)
            {
                // Shown inside the popup, which is opened again with the error
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadAsync(ct);
                ShowCreateModal = true;
                return Page();
            }
        }

        private async Task LoadAsync(CancellationToken ct)
        {
            Users = await _registration.GetUsersAsync(Query.Search, Query.Page, Query.PageSize, ct);
            Restaurants = await _reservations.GetRestaurantsAsync(ct);
        }
    }
}
