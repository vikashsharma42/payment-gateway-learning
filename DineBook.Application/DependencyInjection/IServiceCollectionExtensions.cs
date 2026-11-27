using DineBook.Application.Events;
using DineBook.Application.Security;
using DineBook.Application.Services;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Events;
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

            // Side effects that run after a change is saved (receipts, notifications)
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
            services.AddScoped<IDomainEventSubscriber<PaymentPaidEvent>, PaymentPaidEmailHandler>();

            // Restaurant table reservations
            services.AddScoped<IReservationService, ReservationService>();

            // Menu and food ordering
            services.AddScoped<IMenuService, MenuService>();
            services.AddScoped<IFoodOrderService, FoodOrderService>();

            // Restaurant administration
            services.AddScoped<IRestaurantAdminService, RestaurantAdminService>();

            // Staff workspace: the restaurant and location a staff member is working at
            services.AddScoped<IStaffLocationService, StaffLocationService>();

            // Password hashing
            services.AddSingleton<IPasswordHasher, PasswordHasher>();

            // Sign-up and staff registration
            services.AddScoped<IRegistrationService, RegistrationService>();

            // Customer account: profile, bookings, orders and payments
            services.AddScoped<ICustomerService, CustomerService>();

            // Staff view of a location: bookings and incoming orders
            services.AddScoped<IStaffWorkService, StaffWorkService>();

            // SuperAdmin view across all restaurants
            services.AddScoped<IPlatformMonitorService, PlatformMonitorService>();

            // Security monitor: event log, sign-in lockouts and risk scores
            services.AddScoped<ISecurityService, SecurityService>();

            // First admin account, created from configuration when none exists
            services.AddScoped<AdminAccountSeeder>();

            return services;
        }
    }
}