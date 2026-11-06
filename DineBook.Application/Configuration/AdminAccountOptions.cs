namespace DineBook.Application.Configuration
{
    /// <summary>
    /// The first administrator. When no admin exists, one is created from these values at startup.
    /// Keep the password in user-secrets, never in appsettings.json.
    /// </summary>
    public sealed class AdminAccountOptions
    {
        public const string SectionName = "Admin";

        public string Email { get; set; }

        public string Password { get; set; }

        public string FirstName { get; set; } = "Site";

        public string LastName { get; set; } = "Admin";
    }
}
