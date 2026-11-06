using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DineBook.Application.JWTSecurity;

namespace DineBook.Application.Configuration
{
    public static class JwtOptionsRegistration
    {
        /// <summary>
        /// Binds the Jwt section and fails at startup when the key is missing or shorter than 32 characters.
        /// </summary>
        public static IServiceCollection AddJwtOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<JwtOptions>()
                .Bind(configuration.GetSection(JwtOptions.SectionName))
                .ValidateDataAnnotations()
                .Validate(o => o.TokenExpiry > System.TimeSpan.Zero, "Jwt:TokenExpiry must be greater than zero.")
                .ValidateOnStart();

            return services;
        }
    }
}
