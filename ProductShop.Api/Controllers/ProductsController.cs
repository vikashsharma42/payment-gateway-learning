using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductShop.Infrastructure.Persistence;

namespace ProductShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    // GET api/products
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await db.Products
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .ToListAsync();
        return Ok(products);
    }

    // GET api/products/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
        return product is null ? NotFound() : Ok(product);
    }
}
