using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductShop.Infrastructure.Persistence;

namespace ProductShop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Data Source=productshop.db";

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        return services;
    }
}
