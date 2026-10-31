using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductShop.API.Configuration;
using ProductShop.Application.DependencyInjection;
using ProductShop.Infrastructure.DependencyInjection;
using ProductShop.Payments.DependencyInjection;

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

            // SQLite + ApplicationDbContext
            services.AddApplicationDatabase(_configuration);

            // Cashfree gateway (IPaymentGateway)
            services.AddCashfreePayments(_configuration);

            // Application services (IPaymentService)
            services.AddProductShopApplication();
        }

        // HTTP request pipeline
        public void Configure(IApplicationBuilder app)
        {
            // Middleware: exception handling, HTTPS, routing, CORS
            app.ConfigureProductShopMiddleware(_environment);

            // Endpoints: controllers and OpenAPI
            app.ConfigureProductShopEndpoints(_environment);
        }
    }
}
