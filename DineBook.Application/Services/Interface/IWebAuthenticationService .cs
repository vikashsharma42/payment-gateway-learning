using DineBook.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services.Interface
{
    public interface IWebAuthenticationService
    {
        Task<ApplicationUser> LoginAsync(string email, string password, CancellationToken ct = default);

        Task LogoutAsync();
    }
}