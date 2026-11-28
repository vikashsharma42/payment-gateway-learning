using DineBook.Domain.Entities;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;

namespace DineBook.Application.Security
{
    public static class UserClaimsFactory
    {
        // Restaurant the signed-in user works at or registered with. Absent for admins.
        public const string RestaurantIdClaim = "restaurant_id";

        // Address of that restaurant (its slug). The header uses it to tell whether this account belongs to the restaurant being viewed.
        public const string RestaurantSlugClaim = "restaurant_slug";

        public static IReadOnlyList<Claim> Create(ApplicationUser user, string restaurantSlug = null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.GivenName, user.FirstName),
                new(ClaimTypes.Surname, user.LastName),
                new(ClaimTypes.Role, user.Role.ToString())
            };

            if (user.RestaurantId is { } restaurantId)
                claims.Add(new Claim(RestaurantIdClaim, restaurantId.ToString(CultureInfo.InvariantCulture)));

            if (!string.IsNullOrEmpty(restaurantSlug))
                claims.Add(new Claim(RestaurantSlugClaim, restaurantSlug));

            return claims;
        }

        // The restaurant in the user's cookie, or null for admins
        public static long? RestaurantIdOf(ClaimsPrincipal principal)
        {
            var value = principal?.FindFirst(RestaurantIdClaim)?.Value;
            return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;
        }
    }
}
