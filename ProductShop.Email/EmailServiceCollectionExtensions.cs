using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductShop.Email.Renderer;
using System;

namespace ProductShop.Email
{
    public static class EmailServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the email service, the Razor renderer and IEmailSender.
        /// Email:Enabled=false registers a no-op sender, so no email is sent (views still render).
        /// Enabled=true requires ApiKey and SenderEmail, and the app will not start without them.
        /// </summary>
        public static IServiceCollection AddEmailNotifications(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<EmailOptions>()
                .Bind(configuration.GetSection(EmailOptions.SectionName))
                .Validate(o => !o.Enabled
                               || (!string.IsNullOrWhiteSpace(o.ApiKey)
                                   && !string.IsNullOrWhiteSpace(o.SenderEmail)
                                   && o.SenderEmail.Contains('@')),
                    "When Email:Enabled is true, Email:ApiKey and a valid Email:SenderEmail are required.")
                .ValidateOnStart();

            // Adds IRazorViewEngine and TempData. ProductShop.API only has AddControllers(),
            // so this is needed there too, or PaymentService cannot be created.
            services.AddControllersWithViews();

            services.AddScoped<IRazorViewToStringRenderer, RazorViewToStringRenderer>();
            services.AddScoped<IEmailService, EmailService>();

            var enabled = configuration.GetSection(EmailOptions.SectionName).GetValue<bool>("Enabled");

            if (enabled)
            {
                services.AddHttpClient<IEmailSender, BrevoEmailSender>(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                });
            }
            else
            {
                services.AddSingleton<IEmailSender, DisabledEmailSender>();
            }

            return services;
        }
    }
}