using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DineBook.Domain.ValueObjects;

namespace DineBook.Infrastructure.Database.Configurations
{
    /// <summary>
    /// Column mappings for value objects, shared by every configuration that owns one.
    /// </summary>
    public static class ValueObjectMappings
    {
        public static void MapAddress<TOwner>(OwnedNavigationBuilder<TOwner, Address> builder)
            where TOwner : class
        {
            builder.Property(a => a.Street).HasMaxLength(200).IsRequired();
            builder.Property(a => a.City).HasMaxLength(100).IsRequired();
            builder.Property(a => a.State).HasMaxLength(100).IsRequired();
            builder.Property(a => a.Pincode).HasMaxLength(6).IsRequired();
        }

        public static void MapMoney<TOwner>(OwnedNavigationBuilder<TOwner, Money> builder)
            where TOwner : class
        {
            builder.Property(m => m.Amount).HasPrecision(18, 2);
            builder.Property(m => m.Currency).HasMaxLength(3).IsRequired();
        }
    }
}
