using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DineBook.API.DataObjects.Response;
using DineBook.Infrastructure.Database;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ProductController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAllAsync(CancellationToken ct)
        {
            var products = await _db.Products
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .Select(p => new ProductResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl
                })
                .ToListAsync(ct);

            return Ok(products);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductResponse>> GetByIdAsnc(int id, CancellationToken ct)
        {
            var product = await _db.Products
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new ProductResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl
                })
                .FirstOrDefaultAsync(ct);

            return product is null ? NotFound() : Ok(product);
        }
    }
}