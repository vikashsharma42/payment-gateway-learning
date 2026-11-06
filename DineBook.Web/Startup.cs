
using DineBook.Application.Configuration;
using DineBook.Application.DependencyInjection;
using DineBook.Application.Security;
using DineBook.Application.Services;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Enums;
using DineBook.Email;
using DineBook.Infrastructure.Database;
using DineBook.Infrastructure.DependencyInjection;
using DineBook.Payments.DependencyInjection;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            // Razor Pages
            services.AddRazorPages();

            // Cookie Authentication for Razor Pages
            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Login";

                    options.Cookie.Name = "DineBook.Auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy =
                        CookieSecurePolicy.Always;
                    options.Cookie.SameSite =
                        SameSiteMode.Strict;
                    options.Cookie.IsEssential = true;

                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;
                });

            // First admin configuration
            services.AddAdminAccountOptions(_configuration);

            // Authorization
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(UserRole.Admin)));
            });

            // Rate limiting for checkout POST requests
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

            // HTTP Context Accessor
            services.AddHttpContextAccessor();

            // Web-specific Cookie Authentication Service
            services.AddScoped<IWebAuthenticationService, WebAuthenticationService>();
            services.AddScoped<ICurrentUserProvider, HttpContextCurrentUserProvider>();

            // Site configuration
            services.AddSiteOptions(_configuration);

            // Dining configuration
            services.AddDiningOptions(_configuration);

            // Email notifications
            services.AddEmailNotifications(_configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            // Database scripts must already be applied
            SeedDataAsync(app.ApplicationServices).GetAwaiter().GetResult();

            if (_environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage(new DeveloperExceptionPageOptions
                {
                    SourceCodeLineCount = 5
                });
            }
            else
            {
                app.UseExceptionHandler("/Error/500");
                app.UseStatusCodePagesWithReExecute("/Error/{0}");

                // Configure cookie policies for security
                app.UseCookiePolicy(new CookiePolicyOptions
                {
                    HttpOnly = HttpOnlyPolicy.Always,
                    MinimumSameSitePolicy = SameSiteMode.Strict,
                    Secure = CookieSecurePolicy.Always
                });

                app.UseResponseCaching();
                app.UseHsts();
            }

            app.UseSecurityHeaders();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // Authentication and authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // Rate limiting
            app.UseRateLimiter();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }

        // Client IP address
        private static string ClientKey(HttpContext context) =>
            context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        private async Task SeedDataAsync(IServiceProvider services)
        {
            await using var scope = services.CreateAsyncScope();

            // Seed first admin
            await scope.ServiceProvider.GetRequiredService<AdminAccountSeeder>().SeedAsync();

            // Seed sample restaurants in development
            if (_environment.IsDevelopment())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                await DiningSeeder.SeedAsync(db);
            }
        }
    }
}
