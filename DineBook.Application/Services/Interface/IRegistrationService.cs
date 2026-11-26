using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services.Interface
{
    /// <summary>
    /// Sign-up use cases: public customer registration and admin-created staff accounts.
    /// Callers must authorize the admin operations.
    /// </summary>
    public interface IRegistrationService
    {
        /// <summary>
        /// Creates a Customer account registered with one restaurant, and signs the user in.
        /// </summary>
        Task<ApplicationUser> RegisterCustomerAsync(CustomerRegistrationRequest request, CancellationToken ct = default);

        /// <summary>Creates a Staff, Manager or Admin account. Customer accounts are not created here.</summary>
        Task<ApplicationUser> CreateStaffUserAsync(StaffUserRequest request, CancellationToken ct = default);

        Task<IReadOnlyList<ApplicationUser>> GetUsersAsync(CancellationToken ct = default);

        /// <summary>True when no account uses this email. Used for live validation on the forms.</summary>
        Task<bool> IsEmailAvailableAsync(string email, CancellationToken ct = default);
    }

    /// <summary>RestaurantId is the restaurant the customer registers with. They can then book at any of its locations.</summary>
    public sealed record CustomerRegistrationRequest(
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        string Password,
        long RestaurantId);

    public sealed record StaffUserRequest(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        UserRole Role,
        long? RestaurantId);
}
