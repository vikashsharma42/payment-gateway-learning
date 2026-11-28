using Microsoft.AspNetCore.Builder;

namespace DineBook.Web.Infrastructure
{
    public static class SecurityHeadersExtensions
    {
        // Production policy. Bootstrap is served from this site (wwwroot/lib), so no third-party CDN is needed.
        // Cashfree: the checkout SDK script and frames, and the form that posts the payment to the Cashfree checkout page.
        private const string ContentSecurityPolicy =
            "default-src 'self'; " +
            "script-src 'self' https://sdk.cashfree.com; " +
            "style-src 'self' 'unsafe-inline'; " +
            "font-src 'self'; " +
            "img-src 'self' https: data:; " +
            "connect-src 'self' https://*.cashfree.com; " +
            "frame-src https://*.cashfree.com; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "object-src 'none'; " +
            "form-action 'self' https://*.cashfree.com";

        // Development adds the local tooling connections: Browser Link (SignalR over http) and browser refresh (websocket).
        // Only the local dev server gets them, so the production policy is unchanged.
        private const string DevelopmentConnectSources = " http://localhost:* ws://localhost:* wss://localhost:*";

        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, bool isDevelopment = false)
        {
            var policy = isDevelopment
                ? ContentSecurityPolicy.Replace("connect-src 'self' ", "connect-src 'self'" + DevelopmentConnectSources + " ")
                : ContentSecurityPolicy;

            return app.Use(async (context, next) =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
                headers["Cross-Origin-Opener-Policy"] = "same-origin";
                headers["Content-Security-Policy"] = policy;

                await next();
            });
        }
    }
}