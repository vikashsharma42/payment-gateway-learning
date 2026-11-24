using DineBook.Application.Security;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Infrastructure.Database;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services
{
    public sealed class WebAuthenticationService : IWebAuthenticationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordHasher _hasher;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Lazy<string> _unknownUserHash;

        public WebAuthenticationService(ApplicationDbContext db, IPasswordHasher hasher,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _hasher = hasher;
            _httpContextAccessor = httpContextAccessor;

            _unknownUserHash = new Lazy<string>(() => _hasher.Hash("not-a-real-password"));
        }

        public async Task<ApplicationUser> LoginAsync(string email, string password, CancellationToken ct = default)
        {
            var normalizedEmail = ApplicationUser.NormalizeEmail(email);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

            if (user is null)
            {
                // Same amount of hashing for unknown emails, so timing does not reveal accounts
                _hasher.Verify(password, _unknownUserHash.Value);
                return null;
            }

            var passwordValid = _hasher.Verify(password, user.PasswordHash);
            if (!passwordValid || !user.IsActive)
                return null;

            await SignInAsync(user);
            return user;
        }

        public Task SignInAsync(ApplicationUser user)
        {
            var identity = new ClaimsIdentity(UserClaimsFactory.Create(user), CookieAuthenticationDefaults.AuthenticationScheme);
            var context = CurrentContext();

            return context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }

        public Task LogoutAsync() =>
            CurrentContext().SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        private HttpContext CurrentContext() =>
            _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HTTP context is unavailable.");
    }
}
