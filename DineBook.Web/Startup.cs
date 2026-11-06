using DineBook.Application.Configuration;
using DineBook.Application.DependencyInjection;
using DineBook.Application.Security;
using DineBook.Domain.Enums;
using DineBook.Email;
using DineBook.Infrastructure.Database;
using DineBook.Infrastructure.DependencyInjection;
using DineBook.Payments.DependencyInjection;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace DineBook.Web
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
            // Sign-in with JWT. Jwt:Key lives in user-secrets and must be at least 32 characters.
            // Admin:Email and Admin:Password (user-secrets) create the first admin when none exists.
            services.AddJwtOptions(_configuration);
            services.AddAdminAccountOptions(_configuration);

            // The token travels in an HttpOnly cookie, see JwtBearerSetup
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            services.AddSingleton<IConfigureOptions<JwtBearerOptions>, JwtBearerSetup>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(UserRole.Admin)));
            });

            // Everything under /Admin needs the admin role, except the sign-in and sign-out pages
            services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
                options.Conventions.AllowAnonymousToPage("/Admin/Login");
                options.Conventions.AllowAnonymousToPage("/Admin/Logout");
            });

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
            services.AddDineBookApplication();
            services.AddScoped<ICurrentUserProvider, HttpContextCurrentUserProvider>();

            // Site:PublicBaseUrl
            services.AddSiteOptions(_configuration);

            // Booking rules (party size, notice, slots), validated at startup
            services.AddDiningOptions(_configuration);

            // Receipt emails are sent when a payment is confirmed
            services.AddEmailNotifications(_configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            // Database scripts must already be applied
            SeedDataAsync(app.ApplicationServices).GetAwaiter().GetResult();

            if (!_environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseSecurityHeaders();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // Who the user is (JWT from the cookie), then whether they may see the page
            app.UseAuthentication();
            app.UseAuthorization();

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

        private async Task SeedDataAsync(IServiceProvider services)
        {
            await using var scope = services.CreateAsyncScope();

            // The first admin, from configuration. Does nothing when an admin exists or no values are set.
            await scope.ServiceProvider.GetRequiredService<AdminAccountSeeder>().SeedAsync();

            // Sample restaurants for local testing
            if (_environment.IsDevelopment())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await DiningSeeder.SeedAsync(db);
            }
        }
    }
}
