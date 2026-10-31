namespace ProductShop.Domain.Enum
{
    // Stored as text. The database CHECK constraint must list the same values.
    public enum OrderStatus
    {
        Pending,
        Paid,
        Failed,
        Refunded
    }
}
