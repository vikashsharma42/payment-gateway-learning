using DineBook.Application.Paging;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Web.Dtos;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Admin
{
    // Monitor of every payment and refund across the platform, one page at a time
    public class PaymentsModel : AppPageModel
    {
        private readonly IPlatformMonitorService _monitor;

        public PaymentsModel(IPlatformMonitorService monitor)
        {
            _monitor = monitor;
        }

        public PagedResult<Payment> Payments { get; private set; }

        public async Task<IActionResult> OnGetAsync([FromQuery] PageQuery query, CancellationToken ct)
        {
            Payments = await _monitor.GetPaymentsPageAsync(query.Page, query.PageSize, ct);
            return Page();
        }
    }
}
