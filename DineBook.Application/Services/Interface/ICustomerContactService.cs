using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services.Interface
{
    /// <summary>Contact details the site already knows for a signed-in account, so forms can be filled in for the guest.</summary>
    public interface ICustomerContactService
    {
        /// <summary>Null when the account does not exist. Phone is digits only, empty when the account has no guest record.</summary>
        Task<CustomerContact> GetForUserAsync(long applicationUserId, CancellationToken ct = default);
    }

    public sealed record CustomerContact(string Name, string Phone, string Email);
}
