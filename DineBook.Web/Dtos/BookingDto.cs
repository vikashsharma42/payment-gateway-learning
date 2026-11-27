using System;
using System.ComponentModel.DataAnnotations;

namespace DineBook.Web.Dtos
{
    // The booking form and the date picker: day, party size, chosen time and special requests
    public class BookingDto
    {
        // Day to show slots for. Set by the date picker (query string) and kept by the form (hidden field)
        public DateOnly? Date { get; set; }

        [Range(1, 10, ErrorMessage = "Please choose between 1 and 10 guests.")]
        public int Guests { get; set; } = 2;

        // Chosen slot. Posted as "yyyy-MM-ddTHH:mm:ss" from the time buttons.
        [Required(ErrorMessage = "Please choose a time.")]
        public DateTime StartsAt { get; set; }

        [StringLength(200)]
        [Display(Name = "Special requests (optional)")]
        public string SpecialRequests { get; set; } = string.Empty;
    }
}
