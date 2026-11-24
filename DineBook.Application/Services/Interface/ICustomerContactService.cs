using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services.Interface
{
    /// <summary>Contact details the site already knows for a signed-in account, so forms can be filled in for the customer.</summary>
    public interface ICustomerContactService
    {
        /// <summary>Null when the account does not exist. Phone is digits only, empty for staff accounts without one.</summary>
        Task<CustomerContact> GetForUserAsync(long applicationUserId, CancellationToken ct = default);
    }

    public sealed record CustomerContact(string Name, string Phone, string Email);
}
