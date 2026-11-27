using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Exceptions;
using DineBook.Web.Dtos;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Admin.Restaurants
{
    public class ManageModel : AppPageModel
    {
        private readonly IReservationService _reservations;
        private readonly IRestaurantAdminService _admin;

        public ManageModel(IReservationService reservations, IRestaurantAdminService admin)
        {
            _reservations = reservations;
            _admin = admin;
        }

        [BindProperty(SupportsGet = true)]
        public long Id { get; set; }

        // The location whose tables are shown. Set from the query string (?LocationId=) or from the form.
        [BindProperty(SupportsGet = true)]
        public long LocationId { get; set; }

        // Each form on this page has its own DTO. Their field names are prefixed by the property name.
        [BindProperty]
        public NewLocationDto NewLocation { get; set; } = new();

        [BindProperty]
        public NewTableDto NewTable { get; set; } = new();

        [BindProperty]
        public NewMenuItemDto NewItem { get; set; } = new();

        // Bill charges form. Range is checked by the domain (UpdateCharges), so a bad value shows as a toast.
        [BindProperty]
        public RestaurantChargesDto Rates { get; set; } = new();

        public Restaurant Restaurant { get; private set; }

        // The location shown below. Always one of the restaurant's locations once loaded.
        public Location SelectedLocation { get; private set; }

        public IReadOnlyList<Table> SelectedTables { get; private set; } = Array.Empty<Table>();

        public IReadOnlyList<MenuItem> Menu { get; private set; } = Array.Empty<MenuItem>();

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            if (!await LoadAsync(ct))
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAddLocationAsync(CancellationToken ct)
        {
            ValidateOnly("NewLocation");

            if (!ModelState.IsValid)
            {
                ToastError("Please check the location details and try again.");
                return await ReloadAsync(ct);
            }

            try
            {
                var location = await _admin.AddLocationAsync(Id, new AddLocationRequest(
                    NewLocation.Name, NewLocation.Street, NewLocation.City, NewLocation.State,
                    NewLocation.Country, NewLocation.ZipCode, NewLocation.Phone), ct);

                ToastSuccess($"{location.Name} added. Add its tables next.");
                return RedirectToPage(new { id = Id, locationId = location.Id });
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
                return await ReloadAsync(ct);
            }
        }

        public async Task<IActionResult> OnPostSetLocationActiveAsync(long targetLocationId, bool active, CancellationToken ct)
        {
            try
            {
                await _admin.SetLocationActiveAsync(Id, targetLocationId, active, ct);
                ToastSuccess(active ? "Location reopened." : "Location closed for staff and new bookings.");
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage(new { id = Id, locationId = targetLocationId });
        }

        public async Task<IActionResult> OnPostAddTableAsync(CancellationToken ct)
        {
            ValidateOnly("NewTable");

            if (!ModelState.IsValid)
            {
                ToastError("Please check the table details and try again.");
                return await ReloadAsync(ct);
            }

            try
            {
                await _admin.AddTableAsync(Id, LocationId, NewTable.TableLabel.Trim(), NewTable.TableCapacity, ct);
                ToastSuccess($"Table {NewTable.TableLabel.Trim()} added.");
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage(new { id = Id, locationId = LocationId });
        }

        public async Task<IActionResult> OnPostAddItemAsync(CancellationToken ct)
        {
            ValidateOnly("NewItem");

            if (!ModelState.IsValid)
            {
                ToastError("Please check the menu item details and try again.");
                return await ReloadAsync(ct);
            }

            try
            {
                await _admin.AddMenuItemAsync(Id, NewItem.ItemName, NewItem.ItemDescription, NewItem.ItemCategory, NewItem.ItemPrice, ct);
                ToastSuccess($"{NewItem.ItemName.Trim()} added to the menu.");
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage(new { id = Id, locationId = LocationId });
        }

        public async Task<IActionResult> OnPostChargesAsync(CancellationToken ct)
        {
            ValidateOnly("Rates");

            try
            {
                await _admin.UpdateChargesAsync(Id, Rates.TaxPercent, Rates.SystemAccessFee, ct);
                ToastSuccess("Charges saved. New bills at this restaurant use these rates.");
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage(new { id = Id, locationId = LocationId });
        }

        public async Task<IActionResult> OnPostToggleItemAsync(long itemId, bool available, CancellationToken ct)
        {
            try
            {
                await _admin.SetMenuItemAvailabilityAsync(itemId, available, ct);
                ToastSuccess(available ? "Item is available again." : "Item hidden from customers.");
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage(new { id = Id, locationId = LocationId });
        }

        // Only the submitted form is validated. The other forms are bound too, but their errors must not block this one.
        private void ValidateOnly(string form)
        {
            foreach (var key in ModelState.Keys.Where(k => !k.StartsWith(form + ".", StringComparison.Ordinal)).ToList())
                ModelState.Remove(key);
        }

        private async Task<IActionResult> ReloadAsync(CancellationToken ct)
        {
            return await LoadAsync(ct) ? Page() : NotFound();
        }

        private async Task<bool> LoadAsync(CancellationToken ct)
        {
            Restaurant = await _reservations.GetRestaurantAsync(Id, ct);
            if (Restaurant is null)
                return false;

            // Show the requested location, or the first open one when none is asked for
            SelectedLocation = Restaurant.Locations.FirstOrDefault(l => l.Id == LocationId)
                ?? Restaurant.Locations.OrderBy(l => l.Id).FirstOrDefault(l => l.IsActive)
                ?? Restaurant.Locations.OrderBy(l => l.Id).FirstOrDefault();
            LocationId = SelectedLocation?.Id ?? 0;

            SelectedTables = SelectedLocation is null
                ? Array.Empty<Table>()
                : Restaurant.TablesAt(SelectedLocation.Id).OrderBy(t => t.Label).ToList();

            Menu = await _admin.GetMenuAsync(Id, ct);
            return true;
        }
    }
}
