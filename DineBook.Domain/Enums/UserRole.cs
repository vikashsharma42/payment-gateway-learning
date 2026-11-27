namespace DineBook.Domain.Enums
{
    // Stored as text and written into the sign-in role claim, so the names must stay stable.
    public enum UserRole
    {
        Customer,
        Staff,
        Manager,
        Admin
    }
}
