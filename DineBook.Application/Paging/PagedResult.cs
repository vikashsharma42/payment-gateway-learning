using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Paging
{
    // What a pager needs to know, without the items
    public interface IPagedInfo
    {
        int Page { get; }
        int PageSize { get; }
        int TotalCount { get; }
        int TotalPages { get; }
        bool HasPrevious { get; }
        bool HasNext { get; }
    }

    public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount) : IPagedInfo
    {
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

        public bool HasPrevious => Page > 1;

        public bool HasNext => Page < TotalPages;
    }

    public static class PagingExtensions
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;

        // Runs the count and one page of rows. The query must already be ordered, or pages can repeat or skip rows.
        public static async Task<PagedResult<T>> ToPagedAsync<T>(this IQueryable<T> query, int page, int pageSize, CancellationToken ct = default)
        {
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
            var total = await query.CountAsync(ct);

            // Past the last page shows the last page, not an empty list
            var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            page = Math.Clamp(page, 1, lastPage);

            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return new PagedResult<T>(items, page, pageSize, total);
        }
    }
}
