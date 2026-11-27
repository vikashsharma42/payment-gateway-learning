using System.ComponentModel.DataAnnotations;

namespace DineBook.Web.Dtos
{
    // Paging values from the query string. Every list page that pages its rows uses this.
    public class PageQuery
    {
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;
    }
}
