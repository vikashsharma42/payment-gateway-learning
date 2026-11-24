namespace DineBook.Domain.Enums
{
    // Stored as text and written into the role claim of the sign-in cookie, so the names must stay stable.
    public enum UserRole
    {
        Customer = 0,
        Staff = 1,
        Manager = 2,
        Admin = 3
    }
}
