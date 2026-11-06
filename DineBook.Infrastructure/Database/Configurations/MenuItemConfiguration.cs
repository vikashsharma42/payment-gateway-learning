using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DineBook.Domain.Entities.Menu;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
    {
        public void Configure(EntityTypeBuilder<MenuItem> builder)
        {
            builder.ToTable("MenuItems");
            builder.Property(x => x.Name).HasMaxLength(MenuItem.MaxNameLength).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(MenuItem.MaxDescriptionLength).IsRequired();
            builder.Property(x => x.Category).HasMaxLength(MenuItem.MaxCategoryLength).IsRequired();
            builder.Property(x => x.Price).HasPrecision(10, 2);
            builder.Property(x => x.CreatedDateTime).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedDateTime).IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

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

            // Removed dishes are soft-deleted: hidden from the menu, but kept for past orders
            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.HasIndex(x => new { x.RestaurantId, x.IsAvailable });
        }
    }
}
