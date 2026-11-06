namespace DineBook.Domain.Enums
{
    public enum TableStatus
    {
        // Can be offered for bookings
        Available = 0,
        // Guests are seated right now
        Occupied = 1,
        // Not offered for new bookings (for example under repair)
        OutOfService = 2
    }
}
