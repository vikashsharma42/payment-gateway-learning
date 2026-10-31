using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

namespace ProductShop.API.Configuration
{
    public static class IServiceCollectionExtension
    {
        public const string CorsPolicy = "WebPortal";

        public static IServiceCollection AddApiCoreServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddProblemDetails();

            return services;
        }

        public static IServiceCollection AddApiControllers(this IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            return services;
        }

        public static IServiceCollection AddApiOpenApi(this IServiceCollection services)
        {
            services.AddOpenApi();

            return services;
        }

        public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration)
        {
            var origins = configuration.GetSection("Cors:Origins").Get<string[]>()
                ?? ["http://localhost:3000", "http://localhost:5173", "http://127.0.0.1:5500"];

            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicy, policy =>
                    policy.WithOrigins(origins)
                          .AllowAnyHeader()
                          .AllowAnyMethod());
            });

            return services;
        }
    }
}