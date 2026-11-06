using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DineBook.Domain.Identity;
using DineBook.Domain.Enums;

namespace DineBook.Application.Services.Interface
{
    /// <summary>
    /// Sign-up use cases: public customer registration and admin-created staff accounts.
    /// Callers must authorize the admin operations.
    /// </summary>
    public interface IRegistrationService
    {
        /// <summary>
        /// Creates a Customer account, links it to a guest record (matched by phone), and signs the user in.
        /// </summary>
        Task<AccountSession> RegisterCustomerAsync(CustomerRegistrationRequest request, CancellationToken ct = default);

        /// <summary>Creates a Staff, Manager or Admin account. Customer accounts are not created here.</summary>
        Task<ApplicationUser> CreateStaffUserAsync(StaffUserRequest request, CancellationToken ct = default);

        Task<IReadOnlyList<ApplicationUser>> GetUsersAsync(CancellationToken ct = default);

        /// <summary>True when no account uses this email. Used for live validation on the forms.</summary>
        Task<bool> IsEmailAvailableAsync(string email, CancellationToken ct = default);
    }

    public sealed record CustomerRegistrationRequest(
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        string Password);

    public sealed record StaffUserRequest(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        UserRole Role);
}
