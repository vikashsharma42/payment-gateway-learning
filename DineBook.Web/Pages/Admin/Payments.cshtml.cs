using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Admin
{
    // Monitor of every payment and refund across the platform
    public class PaymentsModel : AppPageModel
    {
        private readonly IPlatformMonitorService _monitor;

        public PaymentsModel(IPlatformMonitorService monitor)
        {
            _monitor = monitor;
        }

        public IReadOnlyList<Payment> Payments { get; private set; } = Array.Empty<Payment>();

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            Payments = await _monitor.GetRecentPaymentsAsync(ct);
            return Page();
        }
    }
}
