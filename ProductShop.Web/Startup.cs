using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProductShop.Application.DependencyInjection;
using ProductShop.Infrastructure.DependencyInjection;
using ProductShop.Payments.DependencyInjection;

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
            // Razor Pages UI
            services.AddRazorPages();

            // SQLite + ApplicationDbContext (product pages read the database directly)
            services.AddApplicationDatabase(_configuration);

            // Cashfree gateway (IPaymentGateway)
            services.AddCashfreePayments(_configuration);

            // Application services (IPaymentService) used by the checkout pages
            services.AddProductShopApplication();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (!_environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}