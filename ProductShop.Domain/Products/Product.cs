using ProductShop.Domain.Common;

namespace ProductShop.Domain.Products;

public class Product
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string ImageUrl { get; private set; } = string.Empty;

    public static Product Create(int id, string name, string description, decimal price, string imageUrl)
    {
        if (price <= 0) throw new DomainException("Price must be greater than zero.");

        return new Product
        {
            Id = id,
            Name = name,
            Description = description,
            Price = price,
            ImageUrl = imageUrl
        };
    }

    public void ChangePrice(decimal newPrice)
    {
        if (newPrice <= 0) throw new DomainException("Price must be greater than zero.");
        Price = newPrice;
    }
}