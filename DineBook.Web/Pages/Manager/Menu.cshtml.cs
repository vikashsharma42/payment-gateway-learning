using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Manager
{
    // Dishes and their categories, prices, descriptions, photos and availability
    public class MenuModel : ManagerPageModel
    {
        public MenuModel(IStaffLocationService staffLocations, IRestaurantAdminService admin)
            : base(staffLocations, admin)
        {
        }

        public IReadOnlyList<MenuItem> Menu { get; private set; } = Array.Empty<MenuItem>();

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            await LoadWorkspaceAsync(null, ct);
            if (Workspace is not null)
                Menu = await Admin.GetMenuAsync(Workspace.Restaurant.Id, ct);

            return Page();
        }

        public Task<IActionResult> OnPostAddAsync(string name, string description, string category, decimal price, CancellationToken ct) =>
            RunAsync(
                ws => Admin.AddMenuItemAsync(ws.Restaurant.Id, (name ?? string.Empty).Trim(), (description ?? string.Empty).Trim(),
                    (category ?? string.Empty).Trim(), price, ct),
                "Dish added to the menu.",
                ct);

        public Task<IActionResult> OnPostUpdateAsync(long menuItemId, string name, string description, decimal price, string imageUrl, CancellationToken ct) =>
            RunAsync(
                ws => Admin.UpdateMenuItemAsync(ws.Restaurant.Id, menuItemId, (name ?? string.Empty).Trim(),
                    (description ?? string.Empty).Trim(), price, (imageUrl ?? string.Empty).Trim(), ct),
                "Dish saved.",
                ct);

        public Task<IActionResult> OnPostAvailabilityAsync(long menuItemId, bool available, CancellationToken ct) =>
            RunAsync(async ws =>
            {
                await EnsureOnMenuAsync(ws.Restaurant.Id, menuItemId, ct);
                await Admin.SetMenuItemAvailabilityAsync(menuItemId, available, ct);
            }, available ? "Dish is available." : "Dish marked unavailable.", ct);

        public Task<IActionResult> OnPostRemoveAsync(long menuItemId, CancellationToken ct) =>
            RunAsync(async ws =>
            {
                await EnsureOnMenuAsync(ws.Restaurant.Id, menuItemId, ct);
                await Admin.RemoveMenuItemAsync(menuItemId, ct);
            }, "Dish removed. Past orders keep it.", ct);

        // The service looks items up by id, so the page confirms the dish is on this restaurant's menu first
        private async Task EnsureOnMenuAsync(long restaurantId, long menuItemId, CancellationToken ct)
        {
            var items = await Admin.GetMenuAsync(restaurantId, ct);
            if (!items.Any(i => i.Id == menuItemId))
                throw new DomainException("That dish is not on this restaurant's menu.");
        }
    }
}
