using DineBook.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services.Interface
{
    /// <summary>
    /// The restaurant of a staff member and the location they are working at. Used by the staff pages.
    /// </summary>
    public interface IStaffLocationService
    {
        /// <summary>
        /// Null when the user has no restaurant. Otherwise the restaurant, its open locations, and the current one.
        /// The current location is <paramref name="locationId"/> when it is one of those open locations of this
        /// restaurant, and the first open location otherwise.
        /// </summary>
        Task<StaffWorkspace> GetWorkspaceAsync(long userId, long? locationId, CancellationToken ct = default);
    }

    public sealed record StaffWorkspace(
        Restaurant Restaurant,
        IReadOnlyList<Location> Locations,
        Location Current);
}
