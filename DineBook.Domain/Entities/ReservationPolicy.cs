namespace DineBook.Domain.Entities
{
    // Booking rules. The application layer builds this from the "Dining" configuration section.
    public sealed record ReservationPolicy(
        int MaxGuests,
        int MinNoticeMinutes,
        int MaxAdvanceDays,
        int DurationMinutes,
        int SlotMinutes);
}
