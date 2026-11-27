using System;
using System.Linq.Expressions;

namespace DineBook.Domain.Specifications
{
    // A named, reusable rule for finding entities. Keeps query rules in the domain instead of repeated lambdas.
    public interface ISpecification<T>
    {
        Expression<Func<T, bool>> Criteria { get; }
    }
}
