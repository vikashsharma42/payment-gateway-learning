namespace ProductShop.Email
{
    /// <summary>
    /// Email settings. Enabled=false turns sending off completely (no network calls).
    /// ApiKey belongs in user-secrets (Email:ApiKey), never in appsettings.json.
    /// </summary>
    public sealed class EmailOptions
    {
        public const string SectionName = "Email";

        public bool Enabled { get; set; }

        public string ApiKey { get; set; }

        // Must be a sender verified in Brevo
        public string SenderEmail { get; set; }

        public string SenderName { get; set; } = "ProductShop";
    }
}