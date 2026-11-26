using DineBook.Application.Services.Interface;
using DineBook.Domain.Exceptions;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Staff
{
    // Staff and managers work at one restaurant and switch between its locations here
    [Authorize(Roles = "Staff,Manager")]
    public class IndexModel : AppPageModel
    {
        // The chosen location is kept in this cookie. It is checked against the user's restaurant on every request.
        public const string LocationCookie = "DineBook.Location";

        private readonly IStaffLocationService _staffLocations;

        public IndexModel(IStaffLocationService staffLocations)
        {
            _staffLocations = staffLocations;
        }

        public StaffWorkspace Workspace { get; private set; }

        [BindProperty]
        public long LocationId { get; set; }

        public async Task<IActionResult> OnGetAsync(long? locationId, CancellationToken ct)
        {
            var requested = locationId ?? ReadCookieLocationId();
            Workspace = await _staffLocations.GetWorkspaceAsync(UserId, requested, ct);

            // The user has no restaurant (or no open location): the page explains that
            if (Workspace is not null)
                LocationId = Workspace.Current.Id;

            return Page();
        }

        public async Task<IActionResult> OnPostSwitchAsync(CancellationToken ct)
        {
            try
            {
                var workspace = await _staffLocations.GetWorkspaceAsync(UserId, LocationId, ct);
                if (workspace is null)
                    throw new DomainException("You are not assigned to a restaurant.");

                // Only an open location of this user's restaurant can be chosen
                if (workspace.Current.Id != LocationId)
                    throw new DomainException("That location is not available.");

                Response.Cookies.Append(LocationCookie, LocationId.ToString(CultureInfo.InvariantCulture), new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    IsEssential = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(30)
                });

                ToastSuccess($"Now working at {workspace.Current.Name}.");
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage();
        }

        private long? ReadCookieLocationId() =>
            long.TryParse(Request.Cookies[LocationCookie], out var id) ? id : null;
    }
}
