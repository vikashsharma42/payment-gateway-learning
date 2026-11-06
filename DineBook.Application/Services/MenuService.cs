using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services
{
    public sealed class MenuService : IMenuService
    {
        private readonly ApplicationDbContext _db;

        public MenuService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<MenuItem>> GetAvailableItemsAsync(long restaurantId, CancellationToken ct = default)
        {
            return await _db.MenuItems.AsNoTracking()
                .Where(m => m.RestaurantId == restaurantId && m.IsAvailable)
                .OrderBy(m => m.Category)
                .ThenBy(m => m.Name)
                .ToListAsync(ct);
        }
    }
}
