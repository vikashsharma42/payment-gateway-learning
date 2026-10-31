using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductShop.Payments.Abstractions;
using ProductShop.Payments.Cashfree;
using ProductShop.Payments.Options;

namespace ProductShop.Payments.Extensions;

public static class PaymentServiceCollectionExtensions
{
    public static IServiceCollection AddCashfreePayments(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CashfreeOptions>()
            .Bind(configuration.GetSection(CashfreeOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.AppId) && !string.IsNullOrWhiteSpace(o.SecretKey),
                "Cashfree:AppId and Cashfree:SecretKey must be set (use user-secrets).")
            .ValidateOnStart();

        services.AddHttpClient<IPaymentGateway, CashfreeGateway>();

        return services;
    }
}