using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DineBook.Application.JWTSecurity
{
    public interface IJwtTokenService
    {
        /// <summary>
        /// Generates a signed JWT from the supplied claims.
        /// </summary>
        Task<string> GenerateJwtTokenAsync(IList<Claim> claims);

        /// <summary>
        /// Validates a JWT, including signature, issuer, audience, and expiration.
        /// </summary>
        Task<bool> ValidateJwtTokenAsync(string token);

        /// <summary>
        /// Invalidates a JWT by adding its JTI to the blacklist.
        /// </summary>
        Task InvalidateTokenAsync(string jti);

        /// <summary>
        /// Determines whether the specified JTI has been blacklisted.
        /// </summary>
        bool IsTokenBlacklisted(string jti);
    }
}
