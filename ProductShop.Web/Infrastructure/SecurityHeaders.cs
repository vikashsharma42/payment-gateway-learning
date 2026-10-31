using Microsoft.AspNetCore.Builder;

namespace ProductShop.Web.Infrastructure
{
    public static class SecurityHeadersExtensions
    {
        // Report-only first: watch the browser console for violations, then switch the header
        // name to "Content-Security-Policy" to enforce it.
        private const string ContentSecurityPolicy =
            "default-src 'self'; " +
            "script-src 'self' https://cdn.jsdelivr.net https://sdk.cashfree.com; " +
            "style-src 'self' https://cdn.jsdelivr.net 'unsafe-inline'; " +
            "font-src 'self' https://cdn.jsdelivr.net; " +
            "img-src 'self' https: data:; " +
            "connect-src 'self' https://*.cashfree.com; " +
            "frame-src https://*.cashfree.com; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "object-src 'none'; " +
            "form-action 'self'";

        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
            app.Use(async (context, next) =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
                headers["Cross-Origin-Opener-Policy"] = "same-origin";
                headers["Content-Security-Policy-Report-Only"] = ContentSecurityPolicy;

                await next();
            });
    }
}