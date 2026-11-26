using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Exceptions;
using DineBook.Web.Infrastructure;

namespace DineBook.Web.Pages.Restaurants
{
    // Public dashboard at /{slug}: locations and the menu are open to everyone.
    // Booking needs a signed-in account, so OnPostAsync sends visitors to sign in first.
    [EnableRateLimiting("checkout")]
    public class BookModel : AppPageModel
    {
        private readonly IReservationService _reservations;
        private readonly IMenuService _menu;

        public BookModel(IReservationService reservations, IMenuService menu)
        {
            _reservations = reservations;
            _menu = menu;
        }

        // The restaurant's web address, for example "spice-garden" in /spice-garden
        [BindProperty(SupportsGet = true)]
        public string Slug { get; set; } = string.Empty;

        // Location switcher: /spice-garden?location=5
        [BindProperty(SupportsGet = true, Name = "location")]
        public long? LocationId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly? Date { get; set; }

        [BindProperty(SupportsGet = true)]
        [Range(1, 10, ErrorMessage = "Party size must be between 1 and 10.")]
        public int Guests { get; set; } = 2;

        // Chosen slot. Posted as "yyyy-MM-ddTHH:mm:ss" from the time buttons.
        [BindProperty]
        [Required(ErrorMessage = "Please choose a time.")]
        public DateTime StartsAt { get; set; }

        [BindProperty]
        [StringLength(200)]
        [Display(Name = "Special requests (optional)")]
        public string SpecialRequests { get; set; } = string.Empty;

        public Restaurant Restaurant { get; private set; }

        // The location shown on the dashboard: the one picked in the switcher, otherwise the first open one
        public Location SelectedLocation { get; private set; }

        public IReadOnlyList<MenuItem> Menu { get; private set; } = Array.Empty<MenuItem>();

        public IReadOnlyList<DateTime> Slots { get; private set; } = Array.Empty<DateTime>();

        public bool IsSignedIn => User.Identity?.IsAuthenticated == true;

        public DateOnly Today => DateOnly.FromDateTime(_reservations.LocalNow());

        // The booking is made in this account's name
        public string BookingName => User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            if (!await LoadAsync(ct))
                return NotFound();

            if (IsSignedIn)
            {
                Date ??= Today;
                await LoadSlotsAsync(ct);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            // Booking is for signed-in accounts. Anyone else is sent to sign in, then back to this page.
            if (!IsSignedIn)
                return Challenge();

            if (!await LoadAsync(ct))
                return NotFound();

            Date ??= Today;

            if (!ModelState.IsValid)
            {
                ToastError("Please check your details and try again.");
                await LoadSlotsAsync(ct);
                return Page();
            }

            try
            {
                var reservation = await _reservations.BookAsync(new BookingRequest(
                    Restaurant.Id,
                    UserId,
                    StartsAt,
                    Guests,
                    (SpecialRequests ?? string.Empty).Trim()),
                    ct);

                ToastSuccess("Your table is booked.");

                // Post/Redirect/Get: a refresh does not book again
                return RedirectToPage("/Reservations/Details", new { slug = Restaurant.Slug, code = reservation.Code });
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
                await LoadSlotsAsync(ct);
                return Page();
            }
        }

        // Loads the restaurant from its slug, the picked location and the menu. False when no restaurant has this slug.
        private async Task<bool> LoadAsync(CancellationToken ct)
        {
            Restaurant = await _reservations.GetRestaurantBySlugAsync(Slug, ct);
            if (Restaurant is null)
                return false;

            var openLocations = Restaurant.ActiveLocations.ToList();
            SelectedLocation = openLocations.FirstOrDefault(l => l.Id == LocationId) ?? openLocations.FirstOrDefault();

            Menu = await _menu.GetAvailableItemsAsync(Restaurant.Id, ct);
            return true;
        }

        private async Task LoadSlotsAsync(CancellationToken ct)
        {
            if (Date is null || Restaurant is null)
                return;

            try
            {
                Slots = await _reservations.GetAvailableSlotsAsync(Restaurant.Id, Date.Value, Guests, ct);
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
                Slots = Array.Empty<DateTime>();
            }
        }
    }
}
