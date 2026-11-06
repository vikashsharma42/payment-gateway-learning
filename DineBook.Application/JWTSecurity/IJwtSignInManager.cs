using System.Threading;
using System.Threading.Tasks;
using DineBook.Domain.Identity;

namespace DineBook.Application.JWTSecurity
{
    public interface IJwtSignInManager
    {
        /// <summary>
        /// Checks the password against the user's stored hash and starts a new session.
        /// Returns null when the password is wrong.
        /// </summary>
        Task<JwtSignInResult> SignInAsync(ApplicationUser appUser, string password);

        Task<JwtSignInResult> SignInExternalAsync(ApplicationUser appUser);

        Task<RefreshResult> RefreshAsync(string refreshToken);

        Task<bool> LogoutAsync(string refreshToken, bool globalLogout = false);

        Task<bool> RevokeAllSessionsAsync(long userId, CancellationToken cancellationToken = default);
    }
}
