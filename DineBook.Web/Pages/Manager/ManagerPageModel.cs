using DineBook.Application.Services.Interface;
using DineBook.Domain.Exceptions;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Manager
{
    // Shared by the manager pages. The restaurant always comes from the signed-in account,
    // so a manager can only change the restaurant they are assigned to.
    [Authorize(Roles = "Manager")]
    public abstract class ManagerPageModel : AppPageModel
    {
        protected ManagerPageModel(IStaffLocationService staffLocations, IRestaurantAdminService admin)
        {
            StaffLocations = staffLocations;
            Admin = admin;
        }

        protected IStaffLocationService StaffLocations { get; }

        protected IRestaurantAdminService Admin { get; }

        // The manager's restaurant with its locations. Null when the account has no restaurant.
        public StaffWorkspace Workspace { get; protected set; }

        protected long? CookieLocationId() =>
            long.TryParse(Request.Cookies[DineBook.Web.Pages.Staff.DashboardModel.LocationCookie], out var id) ? id : null;

        protected async Task LoadWorkspaceAsync(long? locationId, CancellationToken ct) =>
            Workspace = await StaffLocations.GetWorkspaceAsync(UserId, locationId ?? CookieLocationId(), ct);

        // Runs one change for the manager's restaurant. A rule that fails becomes a toast, then the page reloads.
        protected async Task<IActionResult> RunAsync(Func<StaffWorkspace, Task> action, string success, CancellationToken ct)
        {
            try
            {
                var workspace = await StaffLocations.GetWorkspaceAsync(UserId, CookieLocationId(), ct)
                    ?? throw new DomainException("You are not assigned to a restaurant.");

                await action(workspace);
                ToastSuccess(success);
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage();
        }
    }
}
