using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DineBook.Application.Configuration
{
    public static class DiningOptionsRegistration
    {
        /// <summary>
        /// Binds Dining options and fails at startup when a value is not usable.
        /// </summary>
        public static IServiceCollection AddDiningOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<DiningOptions>()
                .Bind(configuration.GetSection(DiningOptions.SectionName))
                .Validate(o => TimeZoneInfo.TryFindSystemTimeZoneById(o.TimeZoneId, out _),
                    "Dining:TimeZoneId is not a valid time zone.")
                .Validate(o => o.MaxGuestsPerBooking >= 1 && o.MaxGuestsPerBooking <= 50,
                    "Dining:MaxGuestsPerBooking must be between 1 and 50.")
                .Validate(o => o.MinNoticeMinutes >= 0 && o.MinNoticeMinutes <= 7 * 24 * 60,
                    "Dining:MinNoticeMinutes must be between 0 and 10080.")
                .Validate(o => o.MaxAdvanceDays >= 1 && o.MaxAdvanceDays <= 365,
                    "Dining:MaxAdvanceDays must be between 1 and 365.")
                .Validate(o => o.DefaultDurationMinutes >= 30 && o.DefaultDurationMinutes <= 300,
                    "Dining:DefaultDurationMinutes must be between 30 and 300.")
                .Validate(o => o.SlotMinutes >= 5 && o.SlotMinutes <= 120,
                    "Dining:SlotMinutes must be between 5 and 120.")
                .ValidateOnStart();

            return services;
        }
    }
}
