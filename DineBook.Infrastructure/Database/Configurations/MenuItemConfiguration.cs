using DineBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
    {
        public void Configure(EntityTypeBuilder<MenuItem> builder)
        {
            builder.ToTable(nameof(MenuItem));
            builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(300);
            builder.Property(x => x.Category).HasMaxLength(50);
            builder.Property(x => x.ImageUrl).HasMaxLength(50);

            // Price is a Money value object, stored as Price_Amount and Price_Currency
            builder.OwnsOne(x => x.Price, a => ValueObjectMappings.MapMoney(a));

            // Many dishes belong to one category. A category is never deleted while it has dishes.
            builder.HasOne(x => x.MenuCategory)
                .WithMany(c => c.MenuItems)
                .HasForeignKey(x => x.MenuCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Many dishes belong to one restaurant
            builder.HasOne(x => x.Restaurant)
                .WithMany(r => r.MenuItems)
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Ignore(x => x.IsDeleted);

            builder.HasIndex(x => new { x.RestaurantId, x.IsAvailable });
        }
    }
}
