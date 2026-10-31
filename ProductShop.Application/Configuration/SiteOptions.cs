namespace ProductShop.Application.Configuration
{
    /// <summary>
    /// Public address of the website. Used to build return URLs, so it never comes from the Host header.
    /// </summary>
    public sealed class SiteOptions
    {
        public const string SectionName = "Site";

        public string PublicBaseUrl { get; set; }
    }
}