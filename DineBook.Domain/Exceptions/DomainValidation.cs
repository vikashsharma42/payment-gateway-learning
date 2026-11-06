using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace DineBook.Domain.Exceptions
{
    /// <summary>
    /// Fluent guard checks, so entities never hand-write their own rules. A failed check throws
    /// <see cref="DomainException"/>. Every check takes an optional message; without one the message is built from
    /// the argument name ("customerName" becomes "Customer name is required.").
    /// <code>DomainValidation&lt;string&gt;.Ensure(name).IsNotNullOrWhiteSpace().HasMaxLength(200);</code>
    /// For a plain condition use <c>DomainValidation&lt;bool&gt;.Ensure(condition).IsTrue("message")</c>.
    /// </summary>
    public sealed class DomainValidation<T>
    {
        private static readonly Regex IdentifierPattern = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$", RegexOptions.Compiled);

        private readonly T _value;
        private readonly string _name;

        private DomainValidation(T value, string name)
        {
            _value = value;
            _name = Humanize(name);
        }

        public static DomainValidation<T> Ensure(T value, [CallerArgumentExpression(nameof(value))] string name = null) =>
            new DomainValidation<T>(value, name);

        // ---- presence ----

        public DomainValidation<T> IsNotNull(string message = null) =>
            Check(_value is not null, message ?? $"{_name} is required.");

        public DomainValidation<T> IsNull(string message = null) =>
            Check(_value is null, message ?? $"{_name} must be empty.");

        public DomainValidation<T> IsNotNullOrWhiteSpace(string message = null) =>
            Check(_value is string text && !string.IsNullOrWhiteSpace(text), message ?? $"{_name} is required.");

        public DomainValidation<T> IsNotDefault(string message = null) =>
            Check(!EqualityComparer<T>.Default.Equals(_value, default), message ?? $"{_name} is required.");

        // ---- text ----

        public DomainValidation<T> HasMaxLength(int maxLength, string message = null) =>
            Check(_value is not string text || text.Trim().Length <= maxLength,
                message ?? $"{_name} can be at most {maxLength} characters.");

        public DomainValidation<T> HasLength(int length, string message = null) =>
            Check(_value is string text && text.Length == length, message ?? $"{_name} must be {length} characters.");

        public DomainValidation<T> IsEmail(string message = null) =>
            Check(_value is string text && IsEmailAddress(text), message ?? $"{_name} must be a valid email.");

        // Counts the digits only, so "+91 98765-43210" is fine
        public DomainValidation<T> IsPhoneNumber(int minDigits, int maxDigits, string message = null)
        {
            var digits = _value is string text ? text.Count(char.IsDigit) : 0;

            return Check(digits >= minDigits && digits <= maxDigits, message ?? $"{_name} must be a valid phone number.");
        }

        public DomainValidation<T> Matches(string pattern, string message = null) =>
            Check(_value is string text && Regex.IsMatch(text.Trim(), pattern, RegexOptions.None, TimeSpan.FromSeconds(1)),
                message ?? $"{_name} is not in the right format.");

        // ---- comparison ----

        public DomainValidation<T> IsEqualTo(T other, string message = null) =>
            Check(EqualityComparer<T>.Default.Equals(_value, other), message ?? $"{_name} must be {other}.");

        public DomainValidation<T> IsNotEqualTo(T other, string message = null) =>
            Check(!EqualityComparer<T>.Default.Equals(_value, other), message ?? $"{_name} cannot be {other}.");

        public DomainValidation<T> IsGreaterThan(T other, string message = null) =>
            Check(Compare(_value, other) > 0, message ?? $"{_name} must be greater than {other}.");

        public DomainValidation<T> IsGreaterThanOrEqualTo(T other, string message = null) =>
            Check(Compare(_value, other) >= 0, message ?? $"{_name} must be at least {other}.");

        public DomainValidation<T> IsLessThanOrEqualTo(T other, string message = null) =>
            Check(Compare(_value, other) <= 0, message ?? $"{_name} must be at most {other}.");

        public DomainValidation<T> IsBetween(T min, T max, string message = null) =>
            Check(Compare(_value, min) >= 0 && Compare(_value, max) <= 0, message ?? $"{_name} must be between {min} and {max}.");

        public DomainValidation<T> IsDateNewerThan(T other, string message = null) =>
            Check(Compare(_value, other) > 0, message ?? $"{_name} must be after {other}.");

        // ---- other ----

        public DomainValidation<T> IsDefinedEnum(string message = null) =>
            Check(_value is Enum value && Enum.IsDefined(value.GetType(), value), message ?? $"{_name} is not valid.");

        public DomainValidation<T> IsTrue(string message) =>
            Check(_value is bool flag && flag, message);

        public DomainValidation<T> IsFalse(string message) =>
            Check(_value is bool flag && !flag, message);

        private DomainValidation<T> Check(bool passed, string message)
        {
            if (!passed)
                throw new DomainException(message);

            return this;
        }

        private static int Compare(T left, T right) => Comparer<T>.Default.Compare(left, right);

        private static bool IsEmailAddress(string text)
        {
            var value = text.Trim();
            var at = value.IndexOf('@', StringComparison.Ordinal);

            return at > 0 && at == value.LastIndexOf('@') && at < value.Length - 1 && !value.Contains(' ', StringComparison.Ordinal);
        }

        // "customerName" becomes "Customer name". Anything that is not a plain name (a method call, "a ?? b") becomes "Value".
        private static string Humanize(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || !IdentifierPattern.IsMatch(name))
                return "Value";

            var text = name[(name.LastIndexOf('.') + 1)..];
            var builder = new StringBuilder();

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (i > 0 && char.IsUpper(c))
                    builder.Append(' ').Append(char.ToLowerInvariant(c));
                else
                    builder.Append(i == 0 ? char.ToUpperInvariant(c) : c);
            }

            return builder.ToString();
        }
    }
}
