using DineBook.Application.Paging;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Web.Dtos;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Admin.Monitoring
{
    // Security log, risky addresses and 24-hour counts. The /Admin folder rule limits this page to administrators.
    public class IndexModel : AppPageModel
    {
        private readonly ISecurityService _security;

        public IndexModel(ISecurityService security)
        {
            _security = security;
        }

        [BindProperty(SupportsGet = true)]
        public PageQuery Query { get; set; } = new();

        public PagedResult<SecurityEvent> Events { get; private set; }

        public IReadOnlyList<RiskSummary> TopRisk { get; private set; } = new List<RiskSummary>();

        public SecuritySummary Summary { get; private set; } = new(0, 0);

        public async Task<IActionResult> OnPostUnblockAsync(string ipAddress, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(ipAddress) || ipAddress.Length > 45)
            {
                ToastError("That address was not recognised.");
                return RedirectToPage();
            }

            await _security.UnblockAddressAsync(ipAddress.Trim(), User.Identity?.Name ?? "administrator", ct);
            ToastSuccess($"{ipAddress.Trim()} is unblocked. Its risk starts again from zero.");
            return RedirectToPage();
        }

        public async Task OnGetAsync(CancellationToken ct)
        {
            Summary = await _security.GetSummaryAsync(ct);
            TopRisk = await _security.GetTopRiskAsync(10, ct);
            Events = await _security.GetEventsPageAsync(Query.Page, Query.PageSize, ct);
        }
    }
}
