using DineBook.Domain.Exceptions;
using DineBook.Infrastructure.Database;
using DineBook.Web.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Infrastructure
{
    // One database read: the restaurant's GST and system access fee, for BillCalculator.
    public sealed class BillingQueries
    {
        private readonly ApplicationDbContext _db;

        public BillingQueries(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<RestaurantCharges> GetChargesAsync(long restaurantId, CancellationToken ct = default)
        {
            return await _db.Restaurants.AsNoTracking()
                .Where(r => r.Id == restaurantId)
                .Select(r => new RestaurantCharges(r.TaxPercent, r.SystemAccessFee, r.TaxType))
                .FirstOrDefaultAsync(ct)
                ?? throw new DomainException("Restaurant not found.");
        }
    }
}
