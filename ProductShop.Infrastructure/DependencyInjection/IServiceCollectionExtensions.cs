using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductShop.Infrastructure.Database;
using System;

namespace ProductShop.Infrastructure.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the database context against SQL Server.
        /// Connection string key: ConnectionStrings:DbConnection.
        /// </summary>
        public static IServiceCollection AddApplicationDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DbConnection");

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
                options.UseSqlServer(
                    connectionString,
                    sqlServer =>
                    {
                        // Retry transient SQL failures
                        sqlServer.EnableRetryOnFailure(
                            maxRetryCount: 10,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);

                        // SQL command timeout
                        sqlServer.CommandTimeout(30);
                    }));

            return services;
        }
    }
}