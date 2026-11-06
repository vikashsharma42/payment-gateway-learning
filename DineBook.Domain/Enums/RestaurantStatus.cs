namespace DineBook.Domain.Enums
{
    public enum RestaurantStatus
    {
        // Open for bookings
        Active = 0,
        // Hidden from new bookings. Existing bookings and history are kept.
        Inactive = 1
    }
}
