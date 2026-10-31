using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductShop.API.Background;
using ProductShop.API.Configuration;
using ProductShop.Application.Configuration;
using ProductShop.Application.DependencyInjection;
using ProductShop.Infrastructure.DependencyInjection;
using ProductShop.Email;
using ProductShop.Payments.DependencyInjection;
using System;
using System.Threading.RateLimiting;

namespace ProductShop.API
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

        // Service registration
        public void ConfigureServices(IServiceCollection services)
        {
            // Core ASP.NET services (ProblemDetails)
            services.AddApiCoreServices();

            // Controllers (JSON enums as strings)
            services.AddApiControllers();

            // OpenAPI document (Development only is mapped in the pipeline)
            services.AddApiOpenApi();

            // CORS allow-list from Cors:Origins
            services.AddApiCors(_configuration);

            // Rate limits used by PaymentController's [EnableRateLimiting] attributes.
            // Without these policies registered, those attributes fail at request time.
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Creating an order is the expensive path: a few per minute per client
                options.AddPolicy("create-order", context => RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

                options.AddPolicy("status", context => RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromSeconds(10),
                        QueueLimit = 0
                    }));

                // Admin actions (refunds): very few per minute per client
                options.AddPolicy("admin", context => RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
            });

            // SQL Server + ApplicationDbContext
            services.AddApplicationDatabase(_configuration);

            // Cashfree gateway (IPaymentGateway)
            services.AddCashfreePayments(_configuration);

            // Application services (IPaymentService)
            services.AddProductShopApplication();

            // Site:PublicBaseUrl, validated at startup
            services.AddSiteOptions(_configuration);

            // Email:Enabled decides between Brevo and a no-op sender
            services.AddEmailNotifications(_configuration);

            // Closes abandoned pending orders (see PendingOrderReaper)
            services.AddHostedService<PendingOrderReaper>();
        }

        // HTTP request pipeline
        public void Configure(IApplicationBuilder app)
        {
            // Payment responses must never be cached by browsers or proxies
            app.Use(async (context, next) =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Cache-Control"] = "no-store";
                await next();
            });

            // Middleware: exception handling, HTTPS, routing, CORS
            app.ConfigureProductShopMiddleware(_environment);

            // After routing, so the rate limit policies can read the endpoint
            app.UseRateLimiter();

            // Endpoints: controllers and OpenAPI
            app.ConfigureProductShopEndpoints(_environment);
        }

        // Behind a proxy this is the proxy's address. Configure forwarded headers before relying on it.
        private static string ClientKey(HttpContext context) =>
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
