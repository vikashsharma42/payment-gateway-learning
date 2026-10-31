using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductShop.Domain.Entities;

namespace ProductShop.Infrastructure.Database.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable(nameof(Product));
            builder.Property(x => x.Price).HasPrecision(18, 2);

            // Seed data. Prices can be changed later in this file or directly in the database.
            builder.HasData(
                new { Id = 1, Name = "Laptop", Description = "14-inch, 16GB RAM, 512GB SSD", Price = 65999m, ImageUrl = "https://picsum.photos/seed/laptop/600/400" },
                new { Id = 2, Name = "Smartphone", Description = "6.5-inch AMOLED, 128GB storage", Price = 24999m, ImageUrl = "https://picsum.photos/seed/phone/600/400" },
                new { Id = 3, Name = "Wireless Headphones", Description = "Noise cancelling, 30h battery", Price = 2999m, ImageUrl = "https://picsum.photos/seed/headphones/600/400" },
                new { Id = 4, Name = "Smartwatch", Description = "Fitness tracking, heart rate", Price = 4499m, ImageUrl = "https://picsum.photos/seed/watch/600/400" },
                new { Id = 5, Name = "Mechanical Keyboard", Description = "RGB backlit, blue switches", Price = 1499m, ImageUrl = "https://picsum.photos/seed/keyboard/600/400" },
                new { Id = 6, Name = "Wireless Mouse", Description = "Ergonomic, 2.4GHz receiver", Price = 799m, ImageUrl = "https://picsum.photos/seed/mouse/600/400" },
                new { Id = 7, Name = "27\" Monitor", Description = "Full HD, 75Hz IPS panel", Price = 12999m, ImageUrl = "https://picsum.photos/seed/monitor/600/400" },
                new { Id = 8, Name = "Tablet", Description = "10.9-inch, 64GB, Wi-Fi", Price = 18999m, ImageUrl = "https://picsum.photos/seed/tablet/600/400" },
                new { Id = 9, Name = "Backpack", Description = "Water resistant, 20L capacity", Price = 1299m, ImageUrl = "https://picsum.photos/seed/backpack/600/400" },
                new { Id = 10, Name = "Water Bottle", Description = "Steel, 1L, keeps cold 24h", Price = 499m, ImageUrl = "https://picsum.photos/seed/bottle/600/400" }
            );
        }
    }
}