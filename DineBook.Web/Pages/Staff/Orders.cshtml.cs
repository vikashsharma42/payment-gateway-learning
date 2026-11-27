using DineBook.Application.Paging;
using DineBook.Web.Dtos;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Exceptions;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Staff
{
    // Food orders for bookings at the staff member's current location
    [Authorize(Roles = "Staff,Manager")]
    public class OrdersModel : AppPageModel
    {
        private readonly IStaffLocationService _staffLocations;
        private readonly IStaffWorkService _work;
        private readonly IFoodOrderService _orders;

        public OrdersModel(IStaffLocationService staffLocations, IStaffWorkService work, IFoodOrderService orders)
        {
            _staffLocations = staffLocations;
            _work = work;
            _orders = orders;
        }

        public StaffWorkspace Workspace { get; private set; }

        // Page number and size from the query string. Action redirects keep them.
        [BindProperty(SupportsGet = true)]
        public PageQuery Query { get; set; } = new();

        public PagedResult<Orders> IncomingOrders { get; private set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            Workspace = await _staffLocations.GetWorkspaceAsync(UserId, ReadCookieLocationId(), ct);
            if (Workspace is not null)
                IncomingOrders = await _work.GetOrdersPageAsync(Workspace.Restaurant.Id, Workspace.Current.Id, Query.Page, Query.PageSize, ct);

            return Page();
        }

        public Task<IActionResult> OnPostServeAsync(long orderId, CancellationToken ct) =>
            ActAsync(orderId, restaurantId => _orders.MarkServedAsync(restaurantId, orderId, ct), "Order marked as served.", ct);

        public Task<IActionResult> OnPostCancelAsync(long orderId, CancellationToken ct) =>
            ActAsync(orderId, restaurantId => _orders.CancelOrderAsync(restaurantId, orderId, ct), "Order cancelled.", ct);

        // Checks the order is at this location, runs the action, then shows the result
        private async Task<IActionResult> ActAsync(long orderId, Func<long, Task> action, string success, CancellationToken ct)
        {
            try
            {
                var workspace = await _staffLocations.GetWorkspaceAsync(UserId, ReadCookieLocationId(), ct)
                    ?? throw new DomainException("You are not assigned to a restaurant.");

                await _work.EnsureOrderAtLocationAsync(workspace.Restaurant.Id, workspace.Current.Id, orderId, ct);
                await action(workspace.Restaurant.Id);
                ToastSuccess(success);
            }
            catch (DomainException ex)
            {
                ToastError(ex.Message);
            }

            return RedirectToPage(new { Query.Page, Query.PageSize });
        }

        private long? ReadCookieLocationId() =>
            long.TryParse(Request.Cookies[DashboardModel.LocationCookie], out var id) ? id : null;
    }
}
