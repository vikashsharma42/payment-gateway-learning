using DineBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class MenuCategoryConfiguration : IEntityTypeConfiguration<MenuCategory>
    {
        public void Configure(EntityTypeBuilder<MenuCategory> builder)
        {
            builder.ToTable(nameof(MenuCategory));

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
