using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Security
{
    /// <summary>
    /// Answers "may this user still use the site?" for every authenticated request without a database
    /// round trip each time. Call <see cref="Invalidate"/> after deactivating or deleting a user.
    /// </summary>
    public interface IUserActiveCache
    {
        Task<bool> IsActiveAsync(long userId, CancellationToken ct = default);

        void Invalidate(long userId);
    }
}
