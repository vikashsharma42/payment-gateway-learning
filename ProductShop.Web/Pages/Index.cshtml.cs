using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductShop.Domain.Products;
using ProductShop.Infrastructure.Persistence;

namespace ProductShop.Web.Pages;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Product> Products { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Products = await db.Products
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .ToListAsync();
    }
}
