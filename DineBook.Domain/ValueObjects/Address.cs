using System.Collections.Generic;
using DineBook.Common.Domain;
using DineBook.Domain.Exceptions;

namespace DineBook.Domain.ValueObjects
{
    public sealed class Address : ValueObject
    {
        private Address(string street, string city, string state, string pincode)
        {
            Street = street;
            City = city;
            State = state;
            Pincode = pincode;
        }

        public string Street { get; }

        public string City { get; }

        public string State { get; }

        public string Pincode { get; }

        public static Address Create(string street, string city, string state, string pincode)
        {
            DomainValidation<string>.Ensure(street).IsNotNullOrWhiteSpace("Street, city and state are required.");
            DomainValidation<string>.Ensure(city).IsNotNullOrWhiteSpace("Street, city and state are required.");
            DomainValidation<string>.Ensure(state).IsNotNullOrWhiteSpace("Street, city and state are required.");
            DomainValidation<string>.Ensure(pincode).Matches(@"^\d{6}$", "Pincode must be 6 digits.");

            return new Address(street.Trim(), city.Trim(), state.Trim(), pincode.Trim());
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
            yield return State;
            yield return Pincode;
        }
    }
}
