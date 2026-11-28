using DineBook.Application.Configuration;
using DineBook.Application.DependencyInjection;
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
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Security.Claims;
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
            // Razor Pages. Everything under /Admin needs the admin role, except sign-in and sign-out.
            services.AddHostedService<SecurityLogCleanupService>();
            services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
                options.Conventions.AllowAnonymousToPage("/Admin/Login");
                options.Conventions.AllowAnonymousToPage("/Admin/Logout");

                // Each dashboard is limited to its own role. Restaurant scope is checked on each page.
                options.Conventions.AuthorizeFolder("/Customer", "CustomerOnly");
                options.Conventions.AuthorizeFolder("/Staff", "StaffArea");
                options.Conventions.AuthorizeFolder("/Manager", "ManagerOnly");
            });

            // Response caching is used by the error pages in production
            services.AddResponseCaching();

            // Short-lived cache for the per-address risk checks
            services.AddMemoryCache();

            // Antiforgery tokens for form posts. The cookie is strict: it is never sent on requests from other sites.
            services.AddAntiforgery(options =>
            {
                options.Cookie.Name = "DineBook.Antiforgery";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.HeaderName = "X-CSRF-TOKEN";
            });

            // Cookie Authentication for Razor Pages
            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Login";
                    options.AccessDeniedPath = "/Login?Denied=true";

                    options.Cookie.Name = "DineBook.Auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.Cookie.IsEssential = true;

                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;
                });

            // Authorization
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(UserRole.Admin)));
                options.AddPolicy("CustomerOnly", policy => policy.RequireRole(nameof(UserRole.Customer)));

                // Staff work the floor and managers can do the same, so both may open the staff pages
                options.AddPolicy("StaffArea", policy => policy.RequireRole(nameof(UserRole.Staff), nameof(UserRole.Manager)));
                options.AddPolicy("ManagerOnly", policy => policy.RequireRole(nameof(UserRole.Manager)));
            });

            // Rate limits. Only POST requests are counted; reads are never limited.
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Sign-in and sign-up, per client address. Strict enough against password guessing,
                // loose enough that a whole office or mobile network behind one address is not locked out.
                options.AddPolicy("auth", context =>
                    context.Request.Method == HttpMethods.Post
                        ? RateLimitPartition.GetFixedWindowLimiter(
                            "ip:" + ClientKey(context),
                            _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 20,
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 0
                            })
                        : RateLimitPartition.GetNoLimiter("read"));

                // Bookings, orders, cancel and payment, per signed-in account (or per address when anonymous).
                // A customer can retry a failed payment straight away.
                options.AddPolicy("checkout", context =>
                    context.Request.Method == HttpMethods.Post
                        ? RateLimitPartition.GetFixedWindowLimiter(
                            ClientPartitionKey(context),
                            _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 30,
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 0
                            })
                        : RateLimitPartition.GetNoLimiter("read"));
            });

            // Cookies are encrypted with these keys. Every server must share them, or a sign-in made on one server
            // cannot be read by another. Set DataProtection:KeysPath to a folder all servers can reach.
            var dataProtection = services.AddDataProtection().SetApplicationName("DineBook");
            var keysPath = _configuration["DataProtection:KeysPath"];
            if (!string.IsNullOrWhiteSpace(keysPath))
                dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));

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

            // Behind a load balancer the visitor's address is in X-Forwarded-For. Without this, every visitor
            // looks like the proxy and shares one rate-limit bucket. Trust the header only behind a proxy that sets it.
            if (_configuration.GetValue<bool>("Proxy:TrustForwardedHeaders"))
            {
                var forwarded = new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                };
                forwarded.KnownIPNetworks.Clear();
                forwarded.KnownProxies.Clear();
                app.UseForwardedHeaders(forwarded);
            }

            app.UseSecurityHeaders(_environment.IsDevelopment());

            // Attack detection, scanner and probe refusal, and per-address risk checks before any page runs
            app.UseMiddleware<SecurityMiddleware>();
            app.UseHttpsRedirection();
            // Images, CSS and JavaScript are cached in the browser for one hour
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = context =>
                {
                    context.Context.Response.Headers["Cache-Control"] = "public, max-age=3600";
                }
            });
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

        // Per-account key for signed-in visitors, otherwise the client address
        private static string ClientPartitionKey(HttpContext context)
        {
            var account = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return account is null ? "ip:" + ClientKey(context) : "user:" + account;
        }

        // Client IP address
        private static string ClientKey(HttpContext context) =>
            context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        private async Task SeedDataAsync(IServiceProvider services)
        {
            await using var scope = services.CreateAsyncScope();

            // Seed first admin (from user-secrets) when no admin exists yet
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