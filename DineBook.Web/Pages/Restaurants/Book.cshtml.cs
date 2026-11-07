using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    [EnableRateLimiting("checkout")]
    public class BookModel : AppPageModel
    {
        private readonly IReservationService _reservations;
        private readonly BookingAccess _access;

        public BookModel(IReservationService reservations, BookingAccess access)
        {
            _reservations = reservations;
            _access = access;
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
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string CustomerName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Phone is required.")]
        [RegularExpression(@"^\+?[0-9 ]{10,15}$", ErrorMessage = "Enter a valid phone number.")]
        [Display(Name = "Phone")]
        public string CustomerPhone { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string CustomerEmail { get; set; } = string.Empty;

        [BindProperty]
        [StringLength(200)]
        [Display(Name = "Special requests (optional)")]
        public string SpecialRequests { get; set; } = string.Empty;

        // True when the form was filled from the account or from this browser's earlier bookings
        public bool Prefilled { get; private set; }

        public Restaurant Restaurant { get; private set; }

        public IReadOnlyList<DateTime> Slots { get; private set; } = Array.Empty<DateTime>();

        public DateOnly Today => DateOnly.FromDateTime(_reservations.LocalNow());

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            Restaurant = await _reservations.GetRestaurantAsync(Id, ct);
            if (Restaurant is null)
                return NotFound();

            Date ??= Today;

            // Guests never retype their details: use the account or the last booking made on this device
            var contact = await _access.PrefillAsync(ct);
            if (contact is not null)
            {
                CustomerName = contact.Name;
                CustomerPhone = contact.Phone;
                CustomerEmail = contact.Email;
                Prefilled = true;
            }

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
                ToastError("Please check the highlighted details.");
                await LoadSlotsAsync(ct);
                return Page();
            }

            var reservation = await _reservations.BookAsync(new BookingRequest(
                Id,
                StartsAt,
                Guests,
                CustomerName.Trim(),
                CustomerPhone.Trim(),
                CustomerEmail.Trim(),
                (SpecialRequests ?? string.Empty).Trim()),
                ct);

            // Remember the booking here so it appears under "My bookings" and the details are prefilled next time
            _access.Remember(reservation, saveContact: true);

            ToastSuccess("Your table is booked.");

            // Post/Redirect/Get: a refresh does not book again
            return RedirectToPage("/Reservations/Details", new { code = reservation.Code });
            
        }

        private async Task LoadSlotsAsync(CancellationToken ct)
        {
            if (Date is null)
                return;

            Slots = await _reservations.GetAvailableSlotsAsync(Id, Date.Value, Guests, ct);
            
        }
    }
}
