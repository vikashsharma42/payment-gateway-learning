namespace DineBook.Domain.Entities.Dining
{
    /// <summary>
    /// Booking rules. The application layer builds this from the "Dining" configuration section.
    /// </summary>
    public sealed record ReservationPolicy(
        int MaxGuests,
        int MinNoticeMinutes,
        int MaxAdvanceDays,
        int DurationMinutes,
        int SlotMinutes);
}
