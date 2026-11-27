using DineBook.Application.Paging;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services.Interface
{
    /// <summary>Security event log, sign-in lockouts and risk scores.</summary>
    public interface ISecurityService
    {
        /// <summary>Saves one security event. Subject is the account concerned, if any.</summary>
        Task RecordAsync(SecurityEventKind kind, string ipAddress, string userAgent, string method, string path,
            string subject, string detail, bool blocked, CancellationToken ct = default);

        /// <summary>True while sign-in is paused for this account or this address after repeated failures.</summary>
        Task<bool> IsSignInLockedAsync(string email, string ipAddress, CancellationToken ct = default);

        /// <summary>Total risk points for this address in the last hour.</summary>
        Task<int> RiskScoreAsync(string ipAddress, CancellationToken ct = default);

        /// <summary>One page of events, newest first.</summary>
        Task<PagedResult<SecurityEvent>> GetEventsPageAsync(int page, int pageSize, CancellationToken ct = default);

        /// <summary>Addresses with the highest risk in the last hour.</summary>
        Task<IReadOnlyList<RiskSummary>> GetTopRiskAsync(int take, CancellationToken ct = default);

        /// <summary>Admin: clears an address's risk and sign-in failures. Only events after this moment count from now on.</summary>
        Task UnblockAddressAsync(string ipAddress, string adminName, CancellationToken ct = default);

        /// <summary>Counts for the last 24 hours.</summary>
        Task<SecuritySummary> GetSummaryAsync(CancellationToken ct = default);

        /// <summary>Deletes events that occurred before the cutoff. Returns how many were removed.</summary>
        Task<int> DeleteEventsOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default);
    }

    public sealed record RiskSummary(string IpAddress, int Score, DateTimeOffset LastSeen);

    public sealed record SecuritySummary(int EventsLast24Hours, int BlockedLast24Hours);
}
