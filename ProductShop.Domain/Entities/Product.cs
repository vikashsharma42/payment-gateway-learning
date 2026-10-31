using ProductShop.Domain.Abstractions;
using ProductShop.Domain.Exceptions;

namespace ProductShop.Domain.Entities
{
    public class Product : BaseEntity
    {
        public Product(string name, string description, decimal price, string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Product name is required.");

            if (price <= 0)
                throw new DomainException("Price must be greater than zero.");

            Name = name.Trim();
            Description = description?.Trim() ?? string.Empty;
            Price = price;
            ImageUrl = imageUrl?.Trim() ?? string.Empty;
        }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public decimal Price { get; private set; }

        public string ImageUrl { get; private set; }

        public void ChangePrice(decimal newPrice)
        {
            if (newPrice <= 0)
                throw new DomainException("Price must be greater than zero.");

            Price = newPrice;
        }
    }
}