using Microsoft.AspNetCore.Http;
using DineBook.Infrastructure.Database;

namespace DineBook.Web.Infrastructure
{
    /// <summary>
    /// Gives the DbContext the signed-in user's email for the CreatedBy and UpdatedBy audit fields.
    /// </summary>
    public sealed class HttpContextCurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
    {
        public string UserName => httpContextAccessor.HttpContext?.User?.Identity?.Name;
    }
}
