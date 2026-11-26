using DineBook.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services.Interface
{
    public interface IWebAuthenticationService
    {
        /// <summary>
        /// Signs in a user. Customers and staff sign in within one restaurant (restaurantId).
        /// Admins are platform-wide: pass null.
        /// </summary>
        Task<ApplicationUser> LoginAsync(string email, string password, long? restaurantId, CancellationToken ct = default);

        Task LogoutAsync();
    }
}