using DineBook.Domain.Entities.Dining;

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

        public ReservationPolicy ToPolicy() => new ReservationPolicy(
            MaxGuestsPerBooking, MinNoticeMinutes, MaxAdvanceDays, DefaultDurationMinutes, SlotMinutes);
    }
}
