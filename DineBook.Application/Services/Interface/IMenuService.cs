using DineBook.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services.Interface
{
    public interface IMenuService
    {
        /// <summary>Items a customer can order at this restaurant right now, grouped by category.</summary>
        Task<IReadOnlyList<MenuItem>> GetAvailableItemsAsync(long restaurantId, CancellationToken ct = default);
    }
}
