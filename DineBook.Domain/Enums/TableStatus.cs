namespace DineBook.Domain.Enums
{
    public enum TableStatus
    {
        // Can be offered for bookings
        Available,
        // Guests are seated right now
        Occupied,
        // Not offered for new bookings (for example under repair)
        OutOfService
    }
}
