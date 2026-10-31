using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductShop.Domain.Products;

namespace ProductShop.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.ImageUrl).HasMaxLength(500);

        // Seed data. Prices can be changed here later, or updated directly in the database.
        builder.HasData(
            Seed(1,  "Laptop",              "14-inch, 16GB RAM, 512GB SSD",   65999, "https://picsum.photos/seed/laptop/600/400"),
            Seed(2,  "Smartphone",          "6.5-inch AMOLED, 128GB storage", 24999, "https://picsum.photos/seed/phone/600/400"),
            Seed(3,  "Wireless Headphones", "Noise cancelling, 30h battery",  2999,  "https://picsum.photos/seed/headphones/600/400"),
            Seed(4,  "Smartwatch",          "Fitness tracking, heart rate",   4499,  "https://picsum.photos/seed/watch/600/400"),
            Seed(5,  "Mechanical Keyboard", "RGB backlit, blue switches",     1499,  "https://picsum.photos/seed/keyboard/600/400"),
            Seed(6,  "Wireless Mouse",      "Ergonomic, 2.4GHz receiver",     799,   "https://picsum.photos/seed/mouse/600/400"),
            Seed(7,  "27\" Monitor",        "Full HD, 75Hz IPS panel",        12999, "https://picsum.photos/seed/monitor/600/400"),
            Seed(8,  "Tablet",              "10.9-inch, 64GB, Wi-Fi",         18999, "https://picsum.photos/seed/tablet/600/400"),
            Seed(9,  "Backpack",            "Water resistant, 20L capacity",  1299,  "https://picsum.photos/seed/backpack/600/400"),
            Seed(10, "Water Bottle",        "Steel, 1L, keeps cold 24h",      499,   "https://picsum.photos/seed/bottle/600/400")
        );
    }

    private static object Seed(int id, string name, string description, decimal price, string imageUrl) => new
    {
        Id = id,
        Name = name,
        Description = description,
        Price = price,
        ImageUrl = imageUrl
    };
}
