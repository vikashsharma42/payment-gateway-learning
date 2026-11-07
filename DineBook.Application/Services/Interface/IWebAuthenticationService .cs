using DineBook.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services.Interface
{
    public interface IWebAuthenticationService
    {
        /// <summary>Checks the password and signs the user in with the site cookie. Null when the details are wrong.</summary>
        Task<ApplicationUser> LoginAsync(string email, string password, CancellationToken ct = default);

        /// <summary>Signs in an account that was just created, with the same cookie as a normal login.</summary>
        Task SignInAsync(ApplicationUser user);

        Task LogoutAsync();
    }
}
