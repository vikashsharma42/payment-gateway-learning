using DineBook.Domain.Enums;

namespace DineBook.Web.Dtos
{
    // The phone number that proves a booking (cancel, food order and pay forms on the booking page)
    public class BookingPhoneDto
    {
        public Country CountryCode { get; set; } = CountryRules.Default;

        public string Phone { get; set; } = string.Empty;
    }
}
