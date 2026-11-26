using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Exceptions;
using DineBook.Web.Infrastructure;

namespace DineBook.Web.Pages.Restaurants
{
    // Booking belongs to a customer account. Signed-out visitors are sent to sign in first.
    [Authorize]
    [EnableRateLimiting("checkout")]
    public class BookModel : AppPageModel
    {
        private readonly IReservationService _reservations;

        public BookModel(IReservationService reservations)
        {
            _reservations = reservations;
        }

        [BindProperty(SupportsGet = true)]
        public long Id { get; set; }

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

        public IReadOnlyList<DateTime> Slots { get; private set; } = Array.Empty<DateTime>();

        public DateOnly Today => DateOnly.FromDateTime(_reservations.LocalNow());

        // The booking is made in this account's name
        public string BookingName => User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            Restaurant = await _reservations.GetRestaurantAsync(Id, ct);
            if (Restaurant is null)
                return NotFound();

            Date ??= Today;
            await LoadSlotsAsync(ct);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            Restaurant = await _reservations.GetRestaurantAsync(Id, ct);
            if (Restaurant is null)
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
                    Id,
                    UserId,
                    StartsAt,
                    Guests,
                    (SpecialRequests ?? string.Empty).Trim()),
                    ct);

                ToastSuccess("Your table is booked.");

                // Post/Redirect/Get: a refresh does not book again
                return RedirectToPage("/Reservations/Details", new { code = reservation.Code });
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
                await LoadSlotsAsync(ct);
                return Page();
            }
        }

        private async Task LoadSlotsAsync(CancellationToken ct)
        {
            if (Date is null)
                return;

            try
            {
                Slots = await _reservations.GetAvailableSlotsAsync(Id, Date.Value, Guests, ct);
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
                Slots = Array.Empty<DateTime>();
            }
        }
    }
}
