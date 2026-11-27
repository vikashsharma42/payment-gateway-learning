using DineBook.Domain.Specifications;
using System.Linq;

namespace DineBook.Infrastructure.Database
{
    public static class SpecificationExtensions
    {
        // Applies a domain specification to an EF Core query
        public static IQueryable<T> Matching<T>(this IQueryable<T> query, ISpecification<T> specification) =>
            query.Where(specification.Criteria);
    }
}
