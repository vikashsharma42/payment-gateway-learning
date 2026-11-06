using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities.Menu;
using DineBook.Domain.Entities.Organization;
using DineBook.Domain.Exceptions;
using DineBook.Web.Infrastructure;

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

        public Restaurant Restaurant { get; private set; }

        public IReadOnlyList<MenuItem> Menu { get; private set; } = Array.Empty<MenuItem>();

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            if (!await LoadAsync(ct))
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAddTableAsync(CancellationToken ct)
        {
            // Both forms share this page, so the other form's fields must not fail this one
            RemoveItemFormErrors();

            if (!ModelState.IsValid)
            {
                ToastError("Check the table details and try again.");
                return await ReloadAsync(ct);
            }

            try
            {
                await _admin.AddTableAsync(Id, TableLabel.Trim(), TableCapacity, ct);
                ToastSuccess($"Table {TableLabel.Trim()} added.");
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage(new { id = Id });
        }

        public async Task<IActionResult> OnPostAddItemAsync(CancellationToken ct)
        {
            RemoveTableFormErrors();

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

            return RedirectToPage(new { id = Id });
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

            return RedirectToPage(new { id = Id });
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

        private async Task<IActionResult> ReloadAsync(CancellationToken ct)
        {
            return await LoadAsync(ct) ? Page() : NotFound();
        }

        private async Task<bool> LoadAsync(CancellationToken ct)
        {
            Restaurant = await _reservations.GetRestaurantAsync(Id, ct);
            if (Restaurant is null)
                return false;

            Menu = await _admin.GetMenuAsync(Id, ct);
            return true;
        }
    }
}
