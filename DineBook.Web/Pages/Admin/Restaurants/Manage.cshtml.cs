using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Exceptions;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        // Add-location form
        [BindProperty]
        [Required(ErrorMessage = "Location name is required.")]
        [StringLength(100)]
        public string NewLocationName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Street is required.")]
        [StringLength(200)]
        public string NewLocationStreet { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "City is required.")]
        [StringLength(100)]
        public string NewLocationCity { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "State is required.")]
        [StringLength(100)]
        public string NewLocationState { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Country is required.")]
        [StringLength(100)]
        public string NewLocationCountry { get; set; } = "India";

        [BindProperty]
        [Required(ErrorMessage = "Zip code is required.")]
        [StringLength(20)]
        public string NewLocationZipCode { get; set; } = string.Empty;

        [BindProperty]
        [StringLength(Location.MaxPhoneLength)]
        public string NewLocationPhone { get; set; } = string.Empty;

        // Add-table form
        [BindProperty]
        [Required(ErrorMessage = "Table label is required.")]
        [StringLength(20)]
        public string TableLabel { get; set; } = string.Empty;

        [BindProperty]
        [Range(1, 20, ErrorMessage = "Capacity must be between 1 and 20.")]
        public int TableCapacity { get; set; } = 2;

        // Add-menu-item form
        [BindProperty]
        [Required(ErrorMessage = "Item name is required.")]
        [StringLength(100)]
        public string ItemName { get; set; } = string.Empty;

        [BindProperty]
        [StringLength(300)]
        public string ItemDescription { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Category is required.")]
        [StringLength(50)]
        public string ItemCategory { get; set; } = string.Empty;

        [BindProperty]
        [Range(0.01, 100000, ErrorMessage = "Price must be between 0.01 and 100000.")]
        public decimal ItemPrice { get; set; }

        // Bill charges form. Range is checked by the domain (UpdateCharges), so a bad value shows as a toast.
        [BindProperty]
        public decimal TaxPercent { get; set; }

        [BindProperty]
        public decimal SystemAccessFee { get; set; }

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
            RemoveTableFormErrors();
            RemoveItemFormErrors();

            if (!ModelState.IsValid)
            {
                ToastError("Check the location details and try again.");
                return await ReloadAsync(ct);
            }

            try
            {
                var location = await _admin.AddLocationAsync(Id, new AddLocationRequest(
                    NewLocationName, NewLocationStreet, NewLocationCity, NewLocationState,
                    NewLocationCountry, NewLocationZipCode, NewLocationPhone), ct);

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
            // Both forms share this page, so the other form's fields must not fail this one
            RemoveItemFormErrors();
            RemoveLocationFormErrors();

            if (!ModelState.IsValid)
            {
                ToastError("Check the table details and try again.");
                return await ReloadAsync(ct);
            }

            try
            {
                await _admin.AddTableAsync(Id, LocationId, TableLabel.Trim(), TableCapacity, ct);
                ToastSuccess($"Table {TableLabel.Trim()} added.");
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage(new { id = Id, locationId = LocationId });
        }

        public async Task<IActionResult> OnPostAddItemAsync(CancellationToken ct)
        {
            RemoveTableFormErrors();
            RemoveLocationFormErrors();

            if (!ModelState.IsValid)
            {
                ToastError("Check the menu item details and try again.");
                return await ReloadAsync(ct);
            }

            try
            {
                await _admin.AddMenuItemAsync(Id, ItemName, ItemDescription, ItemCategory, ItemPrice, ct);
                ToastSuccess($"{ItemName.Trim()} added to the menu.");
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage(new { id = Id, locationId = LocationId });
        }

        public async Task<IActionResult> OnPostChargesAsync(CancellationToken ct)
        {
            RemoveItemFormErrors();
            RemoveTableFormErrors();
            RemoveLocationFormErrors();

            try
            {
                await _admin.UpdateChargesAsync(Id, TaxPercent, SystemAccessFee, ct);
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

        private void RemoveItemFormErrors()
        {
            ModelState.Remove(nameof(ItemName));
            ModelState.Remove(nameof(ItemDescription));
            ModelState.Remove(nameof(ItemCategory));
            ModelState.Remove(nameof(ItemPrice));
        }

        private void RemoveTableFormErrors()
        {
            ModelState.Remove(nameof(TableLabel));
            ModelState.Remove(nameof(TableCapacity));
        }

        private void RemoveLocationFormErrors()
        {
            ModelState.Remove(nameof(NewLocationName));
            ModelState.Remove(nameof(NewLocationStreet));
            ModelState.Remove(nameof(NewLocationCity));
            ModelState.Remove(nameof(NewLocationState));
            ModelState.Remove(nameof(NewLocationCountry));
            ModelState.Remove(nameof(NewLocationZipCode));
            ModelState.Remove(nameof(NewLocationPhone));
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
