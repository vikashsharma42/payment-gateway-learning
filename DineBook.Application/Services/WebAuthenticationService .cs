
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

        public WebAuthenticationService(ApplicationDbContext db, IPasswordHasher hasher,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _hasher = hasher;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ApplicationUser> LoginAsync(string email, string password, long? restaurantId, CancellationToken ct = default)
        {
            var normalizedEmail = ApplicationUser.NormalizeEmail(email);

            // The same email can belong to different restaurants, so a restaurant must be named.
            // Admins have no restaurant and are looked up platform-wide.
            var user = restaurantId is null
                ? await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.RestaurantId == null, ct)
                : await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.RestaurantId == restaurantId.Value, ct);

            if (user is null)
                return null;

            var passwordValid = _hasher.Verify(password, user.PasswordHash);

            if (!passwordValid || !user.IsActive)
                return null;

            // Generate user claims.
            var claims = UserClaimsFactory.Create(user);

            // Create identity.
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Create principal.
            var principal = new ClaimsPrincipal(identity);

            var context = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HTTP context is unavailable.");

            // Issue authentication cookie.
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return user;
        }

        public async Task LogoutAsync()
        {
            var context = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HTTP context is unavailable.");

            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
