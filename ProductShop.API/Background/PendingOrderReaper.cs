using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProductShop.Application.Services.Interface;
using ProductShop.Domain.Entities;
using ProductShop.Domain.Enum;
using ProductShop.Infrastructure.Database;
using ProductShop.Payments.Gateway;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ProductShop.API.Background
{
    /// <summary>
    /// Closes orders the customer abandoned. Old pending orders are checked with Cashfree:
    /// PAID goes through the payment service (so the receipt email is sent),
    /// EXPIRED or TERMINATED marks them failed, anything else is left alone.
    /// </summary>
    public sealed class PendingOrderReaper : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan MinAge = TimeSpan.FromMinutes(30);
        private const int BatchSize = 100;

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<PendingOrderReaper> _logger;

        public PendingOrderReaper(IServiceScopeFactory scopes, ILogger<PendingOrderReaper> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ReapOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Never let one bad run stop the loop
                    _logger.LogError(ex, "Pending order reconciliation failed");
                }
            }
        }

        internal async Task ReapOnceAsync(CancellationToken ct)
        {
            // A hosted service is a singleton, so per-run services come from a scope
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var gateway = scope.ServiceProvider.GetRequiredService<IPaymentGateway>();
            var payments = scope.ServiceProvider.GetRequiredService<IPaymentService>();

            var cutoff = DateTimeOffset.UtcNow - MinAge;
            var stale = await db.Orders
                .Where(o => o.Status == OrderStatus.Pending && o.CreatedAt < cutoff)
                .OrderBy(o => o.Id)
                .Take(BatchSize)
                .ToListAsync(ct);

            foreach (var order in stale)
            {
                try
                {
                    await ReconcileAsync(order, gateway, payments, ct);
                }
                catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
                {
                    // Cashfree failed for this order; try again on the next run
                    _logger.LogWarning(ex, "Could not check order {GatewayOrderId} with Cashfree", order.GatewayOrderId);
                }
            }

            await db.SaveChangesAsync(ct);
        }

        private async Task ReconcileAsync(
            Orders order, IPaymentGateway gateway, IPaymentService payments, CancellationToken ct)
        {
            var remote = await gateway.GetOrderStatusAsync(order.GatewayOrderId, ct);

            switch (remote.Status)
            {
                case "PAID":
                    await payments.MarkPaidAsync(order.GatewayOrderId, ct);
                    break;

                case "EXPIRED":
                case "TERMINATED":
                    order.MarkAsFailed();
                    break;

                default:
                    return; // still open at Cashfree
            }

            _logger.LogInformation(
                "Order {GatewayOrderId} reconciled from Cashfree status {CashfreeStatus}",
                order.GatewayOrderId, remote.Status);
        }
    }
}
