using Microsoft.Extensions.DependencyInjection;
using ProductShop.Application.Services;
using ProductShop.Application.Services.Interface;

namespace ProductShop.Application.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Registers application services (use cases).
        /// </summary>
        public static IServiceCollection AddProductShopApplication(this IServiceCollection services)
        {
            services.AddScoped<IPaymentService, PaymentService>();

            return services;
        }
    }
}