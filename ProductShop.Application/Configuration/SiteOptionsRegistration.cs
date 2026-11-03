using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ProductShop.Application.Configuration
{
    public static class SiteOptionsRegistration
    {
        /// <summary>
        /// Binds Site options and fails at startup if PublicBaseUrl is missing or not an absolute http(s) URL.
        /// </summary>
        public static IServiceCollection AddSiteOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<SiteOptions>()
                .Bind(configuration.GetSection(SiteOptions.SectionName))
                .Validate(
                    o => Uri.TryCreate(o.PublicBaseUrl, UriKind.Absolute, out var uri)
                         && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp),
                    "Site:PublicBaseUrl must be an absolute http(s) URL, for example https://shop.example.com")
                .ValidateOnStart();

            return services;
        }
    }
}
