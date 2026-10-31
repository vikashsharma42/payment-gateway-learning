using ProductShop.Core.Models;

namespace ProductShop.Core.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
}
