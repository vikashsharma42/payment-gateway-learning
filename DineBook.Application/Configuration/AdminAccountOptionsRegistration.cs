using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DineBook.Application.Configuration
{
    public static class AdminAccountOptionsRegistration
    {
        /// <summary>
        /// Binds the Admin section. The values are optional: without them no admin is created,
        /// and an admin must already exist in the database.
        /// </summary>
        public static IServiceCollection AddAdminAccountOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<AdminAccountOptions>()
                .Bind(configuration.GetSection(AdminAccountOptions.SectionName))
                .Validate(
                    o => string.IsNullOrWhiteSpace(o.Password) || o.Password.Length >= 12,
                    "Admin:Password must be at least 12 characters. Store it in user-secrets.")
                .ValidateOnStart();

            return services;
        }
    }
}
