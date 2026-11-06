
using DineBook.Application.Security;
using DineBook.Application.Services;
using DineBook.Application.Services.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace DineBook.Application.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Registers application services (use cases).
        /// </summary>
        public static IServiceCollection AddDineBookApplication(this IServiceCollection services)
        {
            // Legacy single-product payment flow
            services.AddScoped<IPaymentService, PaymentService>();

            // Restaurant table reservations
            services.AddScoped<IReservationService, ReservationService>();

            // Menu and food ordering
            services.AddScoped<IMenuService, MenuService>();
            services.AddScoped<IFoodOrderService, FoodOrderService>();

            // Restaurant administration
            services.AddScoped<IRestaurantAdminService, RestaurantAdminService>();

            // Password hashing
            services.AddSingleton<IPasswordHasher, PasswordHasher>();

            // Sign-up and staff registration
            services.AddScoped<IRegistrationService, RegistrationService>();

            // First admin seeding
            services.AddScoped<AdminAccountSeeder>();

            return services;
        }
    }
}
