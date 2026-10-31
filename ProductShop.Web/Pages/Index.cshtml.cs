using Microsoft.AspNetCore.Mvc.RazorPages;
using ProductShop.Core.Models;
using ProductShop.Core.Services;

namespace ProductShop.Web.Pages;

public class IndexModel(IProductService productService) : PageModel
{
    public IReadOnlyList<Product> Products { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Products = await productService.GetAllAsync();
    }
}