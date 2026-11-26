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
    // /restaurants/{slug}/{locationSlug}/book: booking for signed-in accounts.
    // Visitors who are signed out are sent to sign in, then back to this page.
    [EnableRateLimiting("checkout")]
    public class BookModel : AppPageModel
    {
        private readonly IReservationService _reservations;

        public BookModel(IReservationService reservations)
        {
            _reservations = reservations;
        }

        [BindProperty(SupportsGet = true)]
        public string Slug { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string LocationSlug { get; set; } = string.Empty;

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

        // The open location this booking page is for
        public Location SelectedLocation { get; private set; }

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
                    (SpecialRequests ?? string.Empty).Trim(),
                    SelectedLocation.Id),
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

        // Loads the restaurant from its slug and the open location from its slug. False when either is not found.
        private async Task<bool> LoadAsync(CancellationToken ct)
        {
            Restaurant = await _reservations.GetRestaurantBySlugAsync(Slug, ct);
            if (Restaurant is null || !Restaurant.IsActive)
                return false;

            SelectedLocation = Restaurant.ActiveLocations
                .FirstOrDefault(l => string.Equals(l.Slug, LocationSlug, StringComparison.OrdinalIgnoreCase));
            return SelectedLocation is not null;
        }

        private async Task LoadSlotsAsync(CancellationToken ct)
        {
            if (Date is null || Restaurant is null)
                return;

            try
            {
                Slots = await _reservations.GetAvailableSlotsAsync(Restaurant.Id, Date.Value, Guests, SelectedLocation?.Id, ct);
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
                Slots = Array.Empty<DateTime>();
            }
        }
    }
}
