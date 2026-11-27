using DineBook.Application.Services.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Infrastructure
{
    // Deletes security events older than the retention period, so the log does not grow without limit
    public sealed class SecurityLogCleanupService : BackgroundService
    {
        public const int RetentionDays = 30;

        private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<SecurityLogCleanupService> _logger;

        public SecurityLogCleanupService(IServiceScopeFactory scopes, ILogger<SecurityLogCleanupService> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Let the app finish starting before the first clean-up
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var security = scope.ServiceProvider.GetRequiredService<ISecurityService>();
                    var cutoff = DateTimeOffset.UtcNow.AddDays(-RetentionDays);

                    var removed = await security.DeleteEventsOlderThanAsync(cutoff, stoppingToken);

                    if (removed > 0)
                        _logger.LogInformation("Removed {Count} security events older than {Days} days", removed, RetentionDays);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Security log clean-up failed; it will retry");
                }

                await Task.Delay(Interval, stoppingToken);
            }
        }
    }
}
