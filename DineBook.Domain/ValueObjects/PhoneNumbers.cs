using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DineBook.Domain.ValueObjects
{
    // Phone rules per country. The stored form is the dial code followed by the national number, digits only: 919876543210.
    public static class PhoneNumbers
    {
        // What the guest typed -> stored form. Accepts the national number ("98765 43210"), the full number with a plus
        // ("+91 98765 43210") or the dial code without a plus ("919876543210").
        public static string Resolve(Country country, string typed)
        {
            var rule = country.Rules();
            var raw = (typed ?? string.Empty).Trim();
            var digits = Digits(raw);

            if (raw.StartsWith('+'))
            {
                if (!digits.StartsWith(rule.Dial, StringComparison.Ordinal))
                    throw new DomainException($"The number must start with {rule.DisplayDial} for {rule.CountryName}.");

                digits = digits[rule.Dial.Length..];
            }
            else if (digits.Length > rule.MaxDigits
                && digits.StartsWith(rule.Dial, StringComparison.Ordinal)
                && InRange(rule, digits.Length - rule.Dial.Length))
            {
                digits = digits[rule.Dial.Length..];
            }

            if (!InRange(rule, digits.Length))
                throw Invalid(rule);

            return rule.Dial + digits;
        }

        // The stored value must be the country's dial code followed by a valid national number. Used by the entity.
        public static void EnsureStored(Country country, string storedDigits)
        {
            var rule = country.Rules();
            if (!storedDigits.StartsWith(rule.Dial, StringComparison.Ordinal)
                || !InRange(rule, storedDigits.Length - rule.Dial.Length))
                throw Invalid(rule);
        }

        // The national number without the dial code, for editing in a form. Values not in stored form are returned as they are.
        public static string National(Country country, string stored)
        {
            var digits = Digits(stored);
            var rule = country.Rules();
            if (!digits.StartsWith(rule.Dial, StringComparison.Ordinal)
                || !InRange(rule, digits.Length - rule.Dial.Length))
                return digits;

            return digits[rule.Dial.Length..];
        }

        // For display: the national number in the country's grouping, for example 98765 43210 or 415 555 2671
        public static string Format(Country country, string stored)
        {
            var national = National(country, stored);
            var parts = new List<string>();
            var index = 0;

            foreach (var size in country.Rules().Groups)
            {
                if (index >= national.Length)
                    break;

                var take = Math.Min(size, national.Length - index);
                parts.Add(national.Substring(index, take));
                index += take;
            }

            if (index < national.Length)
                parts.Add(national[index..]);

            return string.Join(" ", parts);
        }

        private static bool InRange(PhoneRule rule, int length) =>
            length >= rule.MinDigits && length <= rule.MaxDigits;

        private static DomainException Invalid(PhoneRule rule)
        {
            var range = rule.MinDigits == rule.MaxDigits
                ? $"{rule.MinDigits} digits"
                : $"{rule.MinDigits} to {rule.MaxDigits} digits";

            return new DomainException($"Enter a valid {rule.CountryName} phone number ({range}).");
        }

        private static string Digits(string text) => new string((text ?? string.Empty).Where(char.IsDigit).ToArray());
    }
}
