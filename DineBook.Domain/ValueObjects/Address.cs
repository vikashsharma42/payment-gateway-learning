using DineBook.Domain.Validation;
using System.Collections.Generic;
<<<<<<< Updated upstream
using DineBook.Common.Domain;
using DineBook.Domain.Exceptions;
=======
using DineBook.Domain.Abstractions;
>>>>>>> Stashed changes

namespace DineBook.Domain.ValueObjects
{
    // Postal address of one restaurant location. The zip code format is loose so that it works in any country.
    public sealed class Address : ValueObject
    {
        public const int MaxStreetLength = 200;
        public const int MaxCityLength = 100;
        public const int MaxStateLength = 100;
        public const int MaxCountryLength = 100;
        public const int MaxZipCodeLength = 20;

        private const string RequiredMessage = "Street, city, state, country and zip code are required.";

        private Address(string street, string city, string state, string country, string zipCode)
        {
            Street = street;
            City = city;
            State = state;
            Country = country;
            ZipCode = zipCode;
        }

        public string Street { get; }

        public string City { get; }

        public string State { get; }

        public string Country { get; }

        public string ZipCode { get; }

        public static Address Create(string street, string city, string state, string country, string zipCode)
        {
            DomainValidation<string>.Ensure(street).IsNotNullOrWhiteSpace(RequiredMessage).HasMaxLength(MaxStreetLength);
            DomainValidation<string>.Ensure(city).IsNotNullOrWhiteSpace(RequiredMessage).HasMaxLength(MaxCityLength);
            DomainValidation<string>.Ensure(state).IsNotNullOrWhiteSpace(RequiredMessage).HasMaxLength(MaxStateLength);
            DomainValidation<string>.Ensure(country).IsNotNullOrWhiteSpace(RequiredMessage).HasMaxLength(MaxCountryLength);
            DomainValidation<string>.Ensure(zipCode).IsNotNullOrWhiteSpace(RequiredMessage).HasMaxLength(MaxZipCodeLength)
                .Matches(@"^[A-Za-z0-9][A-Za-z0-9 \-]{1,18}[A-Za-z0-9]$", "Enter a valid zip or postal code.");

            return new Address(street.Trim(), city.Trim(), state.Trim(), country.Trim(), zipCode.Trim());
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
            yield return State;
            yield return Country;
            yield return ZipCode;
        }
    }
}
