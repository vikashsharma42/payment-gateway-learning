using Microsoft.AspNetCore.Mvc;
using ProductShop.Core.Models;
using ProductShop.Core.Services;

namespace ProductShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductService productService) : ControllerBase
{
    // GET api/products
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Product>>> GetAll()
    {
        var products = await productService.GetAllAsync();
        return Ok(products);
    }

    // GET api/products/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await productService.GetByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }
}
