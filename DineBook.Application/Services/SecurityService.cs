using DineBook.Application.Paging;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Domain.Security;
using DineBook.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services
{
    public sealed class SecurityService : ISecurityService
    {
        private readonly ApplicationDbContext _db;

        public SecurityService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<int> DeleteEventsOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default)
        {
            return await _db.SecurityEvents
                .Where(e => e.OccurredAt < cutoff)
                .ExecuteDeleteAsync(ct);
        }

        public async Task RecordAsync(SecurityEventKind kind, string ipAddress, string userAgent, string method, string path,
            string subject, string detail, bool blocked, CancellationToken ct = default)
        {
            _db.SecurityEvents.Add(new SecurityEvent(
                kind,
                ipAddress,
                userAgent,
                method,
                path,
                (subject ?? string.Empty).Trim().ToLowerInvariant(),
                detail,
                blocked,
                DateTimeOffset.UtcNow));

            await _db.SaveChangesAsync(ct);
        }

        public async Task<bool> IsSignInLockedAsync(string email, string ipAddress, CancellationToken ct = default)
        {
            var since = DateTimeOffset.UtcNow - SignInPolicy.Window;
            var account = (email ?? string.Empty).Trim().ToLowerInvariant();

            var accountFailures = await _db.SecurityEvents.AsNoTracking()
                .CountAsync(e => e.Kind == SecurityEventKind.LoginFailed && e.Subject == account && e.OccurredAt >= since, ct);
            if (accountFailures >= SignInPolicy.MaxFailedPerAccount)
                return true;

            var addressSince = await WindowStartAsync(ipAddress, since, ct);
            var addressFailures = await _db.SecurityEvents.AsNoTracking()
                .CountAsync(e => e.Kind == SecurityEventKind.LoginFailed && e.IpAddress == ipAddress && e.OccurredAt >= addressSince, ct);
            return addressFailures >= SignInPolicy.MaxFailedPerAddress;
        }

        public async Task<int> RiskScoreAsync(string ipAddress, CancellationToken ct = default)
        {
            var since = await WindowStartAsync(ipAddress, DateTimeOffset.UtcNow - RiskScoring.Window, ct);
            return await _db.SecurityEvents.AsNoTracking()
                .Where(e => e.IpAddress == ipAddress && e.OccurredAt >= since)
                .SumAsync(e => (int?)e.Points, ct) ?? 0;
        }

        public Task UnblockAddressAsync(string ipAddress, string adminName, CancellationToken ct = default)
        {
            var who = string.IsNullOrWhiteSpace(adminName) ? "administrator" : adminName.Trim();
            if (who.Length > 200)
                who = who[..200];

            return RecordAsync(SecurityEventKind.AddressUnblocked, ipAddress, string.Empty, "POST", "/Admin/Monitoring",
                string.Empty, $"Unblocked by {who}", false, ct);
        }

        // Counting starts again after the last unblock of this address, but never before the normal window
        private async Task<DateTimeOffset> WindowStartAsync(string ipAddress, DateTimeOffset since, CancellationToken ct)
        {
            var lastUnblock = await _db.SecurityEvents.AsNoTracking()
                .Where(e => e.IpAddress == ipAddress && e.Kind == SecurityEventKind.AddressUnblocked && e.OccurredAt >= since)
                .MaxAsync(e => (DateTimeOffset?)e.OccurredAt, ct);

            return lastUnblock is { } at && at > since ? at : since;
        }

        public Task<PagedResult<SecurityEvent>> GetEventsPageAsync(int page, int pageSize, CancellationToken ct = default) =>
            _db.SecurityEvents.AsNoTracking()
                .OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.Id)
                .ToPagedAsync(page, pageSize, ct);

        public async Task<IReadOnlyList<RiskSummary>> GetTopRiskAsync(int take, CancellationToken ct = default)
        {
            var since = DateTimeOffset.UtcNow - RiskScoring.Window;
            return await _db.SecurityEvents.AsNoTracking()
                .Where(e => e.OccurredAt >= since)
                .GroupBy(e => e.IpAddress)
                .Select(g => new RiskSummary(g.Key, g.Sum(e => e.Points), g.Max(e => e.OccurredAt)))
                .OrderByDescending(r => r.Score)
                .Take(take)
                .ToListAsync(ct);
        }

        public async Task<SecuritySummary> GetSummaryAsync(CancellationToken ct = default)
        {
            var since = DateTimeOffset.UtcNow.AddHours(-24);
            var total = await _db.SecurityEvents.AsNoTracking().CountAsync(e => e.OccurredAt >= since, ct);
            var blocked = await _db.SecurityEvents.AsNoTracking().CountAsync(e => e.OccurredAt >= since && e.Blocked, ct);
            return new SecuritySummary(total, blocked);
        }
    }
}
