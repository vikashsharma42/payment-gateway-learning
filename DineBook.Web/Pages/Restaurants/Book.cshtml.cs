using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Exceptions;
using DineBook.Web.Dtos;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Restaurants
{
    // /{slug}/{locationSlug}/book: booking for signed-in accounts.
    // Visitors who are signed out are sent to sign in, then back to this page.
    [EnableRateLimiting("checkout")]
    public class BookModel : AppPageModel
    {
        private readonly IReservationService _reservations;

        public BookModel(IReservationService reservations)
        {
            _reservations = reservations;
        }

        // Restaurant and location from the address
        [BindProperty(SupportsGet = true)]
        public string Slug { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string LocationSlug { get; set; } = string.Empty;

        // Day, party size, chosen time and requests (see BookingDto)
        [BindProperty(SupportsGet = true)]
        public BookingDto Input { get; set; } = new();

        public Restaurant Restaurant { get; private set; }

        // The open location this booking page is for
        public Location SelectedLocation { get; private set; }

        public IReadOnlyList<DateTime> Slots { get; private set; }

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
                Input.Date ??= Today;
                await LoadSlotsAsync(ct);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            // Booking is for signed-in accounts. Anyone else is sent to sign in, then back to this page.
            if (!IsSignedIn)
                return RedirectToPage("/Restaurants/Login", new { slug = Slug, ReturnUrl = Request.Path.ToString() });

            if (!await LoadAsync(ct))
                return NotFound();

            Input.Date ??= Today;

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
                    Input.StartsAt,
                    Input.Guests,
                    (Input.SpecialRequests ?? string.Empty).Trim(),
                    SelectedLocation.Id),
                    ct);

                ToastSuccess("Your table is booked. See you soon!");

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
            if (Input.Date is null || Restaurant is null)
                return;

            try
            {
                Slots = await _reservations.GetAvailableSlotsAsync(Restaurant.Id, Input.Date.Value, Input.Guests, SelectedLocation?.Id, ct);
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
                Slots = Array.Empty<DateTime>();
            }
        }
    }
}
