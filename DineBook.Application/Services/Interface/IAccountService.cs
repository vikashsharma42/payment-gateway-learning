using System.Threading;
using System.Threading.Tasks;
using DineBook.Application.JWTSecurity;
using DineBook.Domain.Enums;

namespace DineBook.Application.Services.Interface
{
    /// <summary>
    /// Sign-in and sign-out use cases for the Razor Pages site.
    /// </summary>
    public interface IAccountService
    {
        /// <summary>
        /// Checks the email and password and starts a session.
        /// Returns null for an unknown email, a wrong password or a deactivated account, without saying which.
        /// </summary>
        Task<AccountSession> LoginAsync(string email, string password, CancellationToken ct = default);

        /// <summary>
        /// Ends the signed-in user's sessions and blacklists the access token, so neither works again.
        /// </summary>
        Task LogoutAsync(string accessTokenId, string refreshToken);
    }

    public sealed record AccountSession(JwtSignInResult Tokens, UserRole Role);
}
