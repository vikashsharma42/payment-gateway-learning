using ProductShop.Core.Models;

namespace ProductShop.Core.Services;

// Temporary data source. Replace with a database-backed service later.
public class HardcodedProductService : IProductService
{
    private static readonly List<Product> _products =
    [
        new() { Id = 1,  Name = "Laptop", Description = "14-inch, 16GB RAM, 512GB SSD", Price = 65999, ImageUrl = "https://picsum.photos/seed/laptop/600/400" },
        new() { Id = 2,  Name = "Smartphone", Description = "6.5-inch AMOLED, 128GB storage", Price = 24999, ImageUrl = "https://picsum.photos/seed/phone/600/400" },
        new() { Id = 3,  Name = "Wireless Headphones", Description = "Noise cancelling, 30h battery",  Price = 2999,  ImageUrl = "https://picsum.photos/seed/headphones/600/400" },
        new() { Id = 4,  Name = "Smartwatch", Description = "Fitness tracking, heart rate",   Price = 4499,  ImageUrl = "https://picsum.photos/seed/watch/600/400" },
        new() { Id = 5,  Name = "Mechanical Keyboard", Description = "RGB backlit, blue switches",    Price = 1499,  ImageUrl = "https://picsum.photos/seed/keyboard/600/400" },
        new() { Id = 6,  Name = "Wireless Mouse", Description = "Ergonomic, 2.4GHz receiver", Price = 799,   ImageUrl = "https://picsum.photos/seed/mouse/600/400" },
        new() { Id = 7,  Name = "27\" Monitor", Description = "Full HD, 75Hz IPS panel", Price = 12999, ImageUrl = "https://picsum.photos/seed/monitor/600/400" },
        new() { Id = 8,  Name = "Tablet", Description = "10.9-inch, 64GB, Wi-Fi", Price = 18999, ImageUrl = "https://picsum.photos/seed/tablet/600/400" },
        new() { Id = 9,  Name = "Backpack", Description = "Water resistant, 20L capacity",  Price = 1299,  ImageUrl = "https://picsum.photos/seed/backpack/600/400" },
        new() { Id = 10, Name = "Water Bottle", Description = "Steel, 1L, keeps cold 24h", Price = 499,   ImageUrl = "https://picsum.photos/seed/bottle/600/400" },
    ];

    public Task<IReadOnlyList<Product>> GetAllAsync()
        => Task.FromResult<IReadOnlyList<Product>>(_products);

    public Task<Product?> GetByIdAsync(int id)
        => Task.FromResult(_products.FirstOrDefault(p => p.Id == id));
}
