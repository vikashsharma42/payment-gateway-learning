using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProductShop.Application.Configuration;
using ProductShop.Application.DependencyInjection;
using ProductShop.Email;
using ProductShop.Infrastructure.DependencyInjection;
using ProductShop.Payments.DependencyInjection;
using ProductShop.Web.Infrastructure;
using System;
using System.Threading.RateLimiting;

namespace ProductShop.Web
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Razor Pages UI (antiforgery tokens are validated on every POST automatically)
            services.AddRazorPages();

            // TempData carries the pending payment and toasts. Keep it out of scripts' reach and HTTPS-only.
            services.Configure<CookieTempDataProviderOptions>(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.IsEssential = true;
            });

            // Only POSTs are limited, so refreshing the checkout page never counts against the limit
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy("checkout", context =>
                    context.Request.Method == HttpMethods.Post
                        ? RateLimitPartition.GetFixedWindowLimiter(
                            ClientKey(context),
                            _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 5,
                                Window = TimeSpan.FromMinutes(5),
                                QueueLimit = 0
                            })
                        : RateLimitPartition.GetNoLimiter("read"));
            });

            // Database, payments and application services
            services.AddApplicationDatabase(_configuration);
            services.AddCashfreePayments(_configuration);
            services.AddProductShopApplication();

            // Site:PublicBaseUrl and Cloudflare Turnstile verification
            services.AddSiteOptions(_configuration);

            // Receipt emails are sent when a payment is confirmed
            services.AddEmailNotifications(_configuration);
            services.AddHttpClient<TurnstileVerifier>();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (!_environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseSecurityHeaders();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // After UseRouting, so the policy can read the endpoint
            app.UseRateLimiter();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }

        // Behind a proxy this is the proxy's address. Configure forwarded headers before relying on it.
        private static string ClientKey(HttpContext context) =>
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
