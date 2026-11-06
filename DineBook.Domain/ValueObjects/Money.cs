using System.Collections.Generic;
using DineBook.Common.Domain;
using DineBook.Domain.Exceptions;

namespace DineBook.Domain.ValueObjects
{
    public sealed class Money : ValueObject
    {
        public const string DefaultCurrency = "INR";

        private Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        public decimal Amount { get; }

        public string Currency { get; }

        public static Money Create(decimal amount, string currency = DefaultCurrency)
        {
            DomainValidation<decimal>.Ensure(amount).IsGreaterThanOrEqualTo(0m, "Amount cannot be negative.");
            DomainValidation<string>.Ensure(currency).IsNotNullOrWhiteSpace();

            return new Money(amount, currency.Trim().ToUpperInvariant());
        }

        public static Money Zero() => new Money(0m, DefaultCurrency);

        public Money Add(Money other)
        {
            DomainValidation<Money>.Ensure(other, "Amount").IsNotNull();
            DomainValidation<string>.Ensure(other.Currency)
                .IsEqualTo(Currency, "Money in different currencies cannot be added.");

            return new Money(Amount + other.Amount, Currency);
        }

        public Money Multiply(int quantity)
        {
            DomainValidation<int>.Ensure(quantity).IsGreaterThanOrEqualTo(0, "Quantity cannot be negative.");

            return new Money(Amount * quantity, Currency);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }
}
