using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DineBook.Payments.Cashfree;
using DineBook.Payments.Configuration;
using DineBook.Payments.Gateway;
using System;

namespace DineBook.Payments.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Binds Cashfree options (validated at startup) and registers the gateway.
        /// AppId and SecretKey must come from user-secrets, never from appsettings.json.
        /// </summary>
        public static IServiceCollection AddCashfreePayments(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<CashfreeOptions>()
                .Bind(configuration.GetSection(CashfreeOptions.SectionName))
                .Validate(o => !string.IsNullOrWhiteSpace(o.AppId) && !string.IsNullOrWhiteSpace(o.SecretKey),
                    "Cashfree:AppId and Cashfree:SecretKey must be set (use user-secrets).")
                .ValidateOnStart();

            // Timeout on every Cashfree call so a slow gateway cannot hold requests open
            services.AddHttpClient<IPaymentGateway, CashfreeGateway>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(20);
            });

            return services;
        }
    }
}
