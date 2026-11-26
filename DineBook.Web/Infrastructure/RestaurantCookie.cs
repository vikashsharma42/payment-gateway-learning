using DineBook.Domain.ValueObjects;
using Microsoft.AspNetCore.Http;
using System;

namespace DineBook.Web.Infrastructure
{
    // Remembers the restaurant the visitor last opened, so the header can link to that
    // restaurant's orders and profile on pages that have no slug in their address.
    public static class RestaurantCookie
    {
        public const string Name = "DineBook.Restaurant";

        public static void Remember(HttpResponse response, string slug)
        {
            response.Cookies.Append(Name, slug, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
        }

        // The remembered restaurant slug, or null when there is none or it is not a valid slug
        public static string Recall(HttpRequest request)
        {
            if (!request.Cookies.TryGetValue(Name, out var slug))
                return null;

            return RestaurantSlug.IsValid(slug) ? slug : null;
        }
    }
}
