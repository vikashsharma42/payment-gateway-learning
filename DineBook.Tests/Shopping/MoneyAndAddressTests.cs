using DineBook.Common.ValueObjects;
using DineBook.Domain.Exceptions;
using Xunit;

namespace DineBook.Tests.Shopping
{
    public class MoneyAndAddressTests
    {
        [Fact]
        public void Money_rejects_negative_amount()
        {
            Assert.Throws<DomainException>(() => Money.Create(-1m));
        }

        [Fact]
        public void Money_multiply_scales_the_amount()
        {
            Assert.Equal(750m, Money.Create(250m).Multiply(3).Amount);
        }

        [Fact]
        public void Money_with_the_same_amount_and_currency_is_equal()
        {
            Assert.Equal(Money.Create(10m), Money.Create(10m));
        }

        [Fact]
        public void Address_rejects_a_pincode_that_is_not_six_digits()
        {
            Assert.Throws<DomainException>(() => Address.Create("1 Road", "Delhi", "Delhi", "11001"));
        }

        [Fact]
        public void Address_is_trimmed_when_valid()
        {
            var address = Address.Create(" 1 Road ", "Delhi", "Delhi", "110001");

            Assert.Equal("1 Road", address.Street);
        }
    }
}
