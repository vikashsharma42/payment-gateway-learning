using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Manager
{
    // Tables of one location: add, rename, change seats, and remove
    public class TablesModel : ManagerPageModel
    {
        public TablesModel(IStaffLocationService staffLocations, IRestaurantAdminService admin)
            : base(staffLocations, admin)
        {
        }

        public IReadOnlyList<Table> Tables { get; private set; } = Array.Empty<Table>();

        public async Task<IActionResult> OnGetAsync(long? locationId, CancellationToken ct)
        {
            await LoadWorkspaceAsync(locationId, ct);
            if (Workspace is not null)
                Tables = Workspace.Restaurant.TablesAt(Workspace.Current.Id).OrderBy(t => t.Label).ToList();

            return Page();
        }

        public Task<IActionResult> OnPostAddAsync(long locationId, string label, int capacity, CancellationToken ct) =>
            RunAsync(
                ws => Admin.AddTableAsync(ws.Restaurant.Id, locationId, (label ?? string.Empty).Trim(), capacity, ct),
                "Table added.",
                ct);

        public Task<IActionResult> OnPostRenameAsync(long tableId, string label, CancellationToken ct) =>
            RunAsync(
                ws => Admin.RenameTableAsync(ws.Restaurant.Id, tableId, (label ?? string.Empty).Trim(), ct),
                "Table renamed.",
                ct);

        public Task<IActionResult> OnPostCapacityAsync(long tableId, int capacity, CancellationToken ct) =>
            RunAsync(
                ws => Admin.ChangeTableCapacityAsync(ws.Restaurant.Id, tableId, capacity, ct),
                "Seats updated.",
                ct);

        public Task<IActionResult> OnPostRemoveAsync(long tableId, CancellationToken ct) =>
            RunAsync(
                ws => Admin.RemoveTableAsync(ws.Restaurant.Id, tableId, ct),
                "Table removed. Past bookings keep it.",
                ct);
    }
}
