namespace DineBook.Domain.Enums
{
    // Stored as text and written into the JWT role claim, so the names must stay stable.
    public enum UserRole
    {
        Admin,
        Manager,
        Staff
        Customer,
    }
}
