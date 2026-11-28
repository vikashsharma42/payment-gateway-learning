using System;
using System.Collections.Generic;

namespace DineBook.Domain.Enums
{
    // A country's phone rules: its name, dial code (digits only, "91"), how many national digits follow it,
    // how to group them for display, and an example number for the placeholder.
    public sealed record PhoneRule(string CountryName, string Dial, int MinDigits, int MaxDigits, int[] Groups, string Example)
    {
        // "+91" for display
        public string DisplayDial => "+" + Dial;
    }

    public static class CountryRules
    {
        // Used when nothing else is chosen
        public const Country Default = Country.IN;

        // Every country, in the order shown in the country picker
        public static IReadOnlyList<Country> All { get; } = Enum.GetValues<Country>();

        // Phone rules for each country. Add a rule here whenever a value is added to the Country enum.
        public static PhoneRule Rules(this Country country) => country switch
        {
            Country.IN => new("India", "91", 10, 10, [5, 5], "98765 43210"),
            Country.US => new("United States", "1", 10, 10, [3, 3, 4], "415 555 2671"),
            Country.CA => new("Canada", "1", 10, 10, [3, 3, 4], "416 555 0123"),
            Country.GB => new("United Kingdom", "44", 10, 10, [4, 6], "7400 123456"),
            Country.AE => new("United Arab Emirates", "971", 9, 9, [2, 3, 4], "50 123 4567"),
            Country.SA => new("Saudi Arabia", "966", 9, 9, [2, 3, 4], "50 123 4567"),
            Country.QA => new("Qatar", "974", 8, 8, [4, 4], "3012 3456"),
            Country.OM => new("Oman", "968", 8, 8, [4, 4], "9212 3456"),
            Country.KW => new("Kuwait", "965", 8, 8, [4, 4], "5012 3456"),
            Country.SG => new("Singapore", "65", 8, 8, [4, 4], "8123 4567"),
            Country.MY => new("Malaysia", "60", 9, 10, [2, 3, 4], "12 345 6789"),
            Country.TH => new("Thailand", "66", 9, 9, [2, 3, 4], "81 234 5678"),
            Country.ID => new("Indonesia", "62", 9, 12, [3, 4, 4], "812 3456 7890"),
            Country.PH => new("Philippines", "63", 10, 10, [3, 3, 4], "917 123 4567"),
            Country.JP => new("Japan", "81", 10, 10, [2, 4, 4], "90 1234 5678"),
            Country.NP => new("Nepal", "977", 10, 10, [3, 3, 4], "984 123 4567"),
            Country.BD => new("Bangladesh", "880", 10, 10, [4, 6], "1712 345678"),
            Country.LK => new("Sri Lanka", "94", 9, 9, [2, 3, 4], "71 123 4567"),
            Country.PK => new("Pakistan", "92", 10, 10, [3, 7], "300 1234567"),
            Country.AU => new("Australia", "61", 9, 9, [3, 3, 3], "412 345 678"),
            Country.DE => new("Germany", "49", 10, 11, [4, 7], "1512 3456789"),
            Country.FR => new("France", "33", 9, 9, [1, 2, 2, 2, 2], "6 12 34 56 78"),
            Country.IT => new("Italy", "39", 9, 10, [3, 3, 4], "312 345 6789"),
            Country.ES => new("Spain", "34", 9, 9, [3, 2, 2, 2], "612 34 56 78"),
            _ => throw new ArgumentOutOfRangeException(nameof(country), country, "No phone rule for this country."),
        };
    }
}
