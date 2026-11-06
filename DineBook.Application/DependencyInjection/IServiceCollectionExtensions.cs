using Microsoft.Extensions.DependencyInjection;
using DineBook.Application.JWTSecurity;
using DineBook.Application.Security;
using DineBook.Application.Services;
using DineBook.Application.Services.Interface;

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

            // Menu and food ordering for a booking
            services.AddScoped<IMenuService, MenuService>();
            services.AddScoped<IFoodOrderService, FoodOrderService>();

            // Admin: restaurant, table and menu management
            services.AddScoped<IRestaurantAdminService, RestaurantAdminService>();

            // Sign-in: JWT access and refresh tokens, password hashing, and the active-user check.
            // Options come from AddJwtOptions and AddAdminAccountOptions (see the host's Startup).
            services.AddMemoryCache();
            services.AddHttpContextAccessor();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IRefreshTokenStore, DbRefreshTokenStore>();
            services.AddScoped<IJwtSignInManager, JwtSignInManager>();
            services.AddScoped<IUserActiveCache, UserActiveCache>();
            services.AddScoped<IAccountService, AccountService>();

            // Sign-up: public customer registration and admin-created staff accounts
            services.AddScoped<IRegistrationService, RegistrationService>();
            services.AddScoped<AdminAccountSeeder>();

            return services;
        }
    }
}
