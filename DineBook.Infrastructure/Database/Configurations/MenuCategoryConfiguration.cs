using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DineBook.Domain.Entities.Menu;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class MenuCategoryConfiguration : IEntityTypeConfiguration<MenuCategory>
    {
        public void Configure(EntityTypeBuilder<MenuCategory> builder)
        {
            builder.ToTable("MenuCategories");
            builder.Property(x => x.Name).HasMaxLength(MenuCategory.MaxNameLength).IsRequired();
            builder.Property(x => x.CreatedDateTime).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedDateTime).IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            // Many categories belong to one restaurant. Deleting a restaurant removes its categories.
            builder.HasOne(x => x.Restaurant)
                .WithMany(r => r.MenuCategories)
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.RestaurantId, x.Name }).IsUnique();

            builder.Navigation(nameof(MenuCategory.MenuItems)).UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
