using DineBook.Application.JWTSecurity;
using DineBook.Application.Security;
using DineBook.Application.Services.Interface;
using DineBook.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services
{
    public sealed class AccountService : IAccountService
    {
        private readonly ApplicationDbContext _db;
        private readonly IJwtSignInManager _signIn;
        private readonly IJwtTokenService _tokens;
        private readonly IPasswordHasher _hasher;
        private readonly Lazy<string> _unknownUserHash;

        public AccountService(
            ApplicationDbContext db,
            IJwtSignInManager signIn,
            IJwtTokenService tokens,
            IPasswordHasher hasher)
        {
            _db = db;
            _signIn = signIn;
            _tokens = tokens;
            _hasher = hasher;
            _unknownUserHash = new Lazy<string>(() => hasher.Hash("not-a-real-password"));
        }

        public async Task<AccountSession> LoginAsync(string email, string password, CancellationToken ct = default)
        {
            var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalized, ct);

            if (user is null)
            {
                // Same work as a real check, so the response time does not reveal whether the email exists
                _hasher.Verify(password, _unknownUserHash.Value);
                return null;
            }

            var tokens = await _signIn.SignInAsync(user, password);
            return tokens is null ? null : new AccountSession(tokens, user.Role);
        }

        public async Task LogoutAsync(string accessTokenId, string refreshToken)
        {
            // One session per user, so ending them all is the same as ending this one
            await _signIn.LogoutAsync(refreshToken, globalLogout: true);
            await _tokens.InvalidateTokenAsync(accessTokenId);
        }
    }
}
