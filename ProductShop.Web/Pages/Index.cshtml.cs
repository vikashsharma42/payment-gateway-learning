using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductShop.Domain.Entities;
using ProductShop.Infrastructure.Database;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProductShop.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public IReadOnlyCollection<Products> Products { get; private set; } = [];

        public async Task OnGetAsync(CancellationToken ct)
        {
            Products = await _db.Products
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .ToListAsync(ct);
        }
    }
}