using System.Linq;
using System.Text.RegularExpressions;

namespace DineBook.Domain.ValueObjects
{
    // The web address of a restaurant, for example "spice-garden" in /spice-garden/
    public static class RestaurantSlug
    {
        public const int MaxLength = 60;

        // Room left for a "-2" style suffix when two restaurants have similar names
        private const int BaseMaxLength = 50;

        // Words used by the site's own pages, so a restaurant cannot take them
        private static readonly string[] Reserved =
        {
            "admin", "api", "bookings", "checkout", "css", "error", "img", "index", "js", "lib",
            "login", "logout", "orders", "payment", "registration", "reservations"
        };

        private static readonly Regex Valid = new Regex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

        public static string FromName(string name)
        {
            var text = (name ?? string.Empty).Trim().ToLowerInvariant();
            var slug = Regex.Replace(text, "[^a-z0-9]+", "-").Trim('-');

            if (slug.Length > BaseMaxLength)
                slug = slug.Substring(0, BaseMaxLength).TrimEnd('-');

            if (slug.Length == 0)
                slug = "restaurant";

            if (Reserved.Contains(slug))
                slug += "-restaurant";

            return slug;
        }

        public static bool IsValid(string slug) =>
            !string.IsNullOrEmpty(slug)
            && slug.Length <= MaxLength
            && Valid.IsMatch(slug)
            && !Reserved.Contains(slug);

        public static string WithSuffix(string slug, int number) => $"{slug}-{number}";
    }
}
