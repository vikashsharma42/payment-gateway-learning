using DineBook.Domain.Entities;

namespace DineBook.Application.Configuration
{
    /// <summary>
    /// Booking rules. Bound from the "Dining" section of appsettings.json.
    /// </summary>
    public sealed class DiningOptions
    {
        public const string SectionName = "Dining";

        // Restaurant local time. Used for "now", notice and advance-booking checks.
        public string TimeZoneId { get; set; } = "Asia/Kolkata";

        public int MaxGuestsPerBooking { get; set; } = 10;

        public int MinNoticeMinutes { get; set; } = 60;

        public int MaxAdvanceDays { get; set; } = 30;

        public int DefaultDurationMinutes { get; set; } = 90;

        // Time slots are offered every SlotMinutes
        public int SlotMinutes { get; set; } = 30;

        // Booking advance in rupees. Deducted from the food bill once per booking, at checkout.
        public decimal BookingAdvance { get; set; } = 500m;

        // Kept from the deposit when a booking is cancelled. The rest is refunded.
        public decimal CancellationFee { get; set; } = 100m;

        public ReservationPolicy ToPolicy() => new ReservationPolicy(
            MaxGuestsPerBooking, MinNoticeMinutes, MaxAdvanceDays, DefaultDurationMinutes, SlotMinutes);
    }
}
