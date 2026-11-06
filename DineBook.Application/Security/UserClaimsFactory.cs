
using DineBook.Domain.Entities;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;

namespace DineBook.Application.Security
{
    public static class UserClaimsFactory
    {
        public static IReadOnlyList<Claim> Create(ApplicationUser user)
        {
            return new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.GivenName, user.FirstName),
                new(ClaimTypes.Surname, user.LastName),
                new(ClaimTypes.Role, user.Role.ToString())
            };
        }
    }
}