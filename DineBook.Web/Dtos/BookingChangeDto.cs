using System.ComponentModel.DataAnnotations;

namespace DineBook.Web.Dtos
{
    // The edit form on the booking page: the phone number proves the booking, then the party size and special requests
    public class BookingChangeDto
    {
        [Required(ErrorMessage = "Enter the phone number used for this booking.")]
        [StringLength(20)]
        public string Phone { get; set; }

        [Range(1, 10, ErrorMessage = "Please choose between 1 and 10 guests.")]
        public int Guests { get; set; } = 2;

        [StringLength(200, ErrorMessage = "Special requests can be at most 200 characters.")]
        public string SpecialRequests { get; set; }
    }
}
