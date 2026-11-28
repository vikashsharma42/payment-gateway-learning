using DineBook.Domain.Enums;
using DineBook.Domain.ValueObjects;

namespace DineBook.Web.Helpers
{
    // Shows a customer's phone with the flag of their country, for example "🇮🇳 +91 98765 43210".
    public static class PhoneDisplay
    {
        private const int RegionalIndicatorA = 0x1F1E6;

        // The flag from the country's ISO code. Each letter becomes a regional indicator symbol, which browsers draw as a flag.
        public static string Flag(Country country)
        {
            var code = country.ToString();
            return char.ConvertFromUtf32(RegionalIndicatorA + (code[0] - 'A'))
                 + char.ConvertFromUtf32(RegionalIndicatorA + (code[1] - 'A'));
        }

        public static string Show(Country? country, string storedPhone)
        {
            if (string.IsNullOrWhiteSpace(storedPhone))
                return string.Empty;

            var chosen = country ?? CountryRules.Default;
            return $"{Flag(chosen)} {chosen.Rules().DisplayDial} {PhoneNumbers.Format(chosen, storedPhone)}";
        }
    }
}
