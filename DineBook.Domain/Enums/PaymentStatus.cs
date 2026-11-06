namespace DineBook.Domain.Enums
{
    // Stored as text. The database CHECK constraint must list the same values.
    public enum PaymentStatus
    {
        Pending,
        Paid,
        Failed,
        Refunded
    }
}
