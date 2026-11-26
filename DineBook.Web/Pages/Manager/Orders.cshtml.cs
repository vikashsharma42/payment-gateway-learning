using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Manager
{
    // Food orders across all locations, with their payment state
    public class OrdersModel : ManagerPageModel
    {
        private readonly IStaffWorkService _work;
        private readonly IFoodOrderService _orders;

        public OrdersModel(
            IStaffLocationService staffLocations,
            IRestaurantAdminService admin,
            IStaffWorkService work,
            IFoodOrderService orders)
            : base(staffLocations, admin)
        {
            _work = work;
            _orders = orders;
        }

        public IReadOnlyList<Orders> AllOrders { get; private set; } = Array.Empty<Orders>();

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            await LoadWorkspaceAsync(null, ct);
            if (Workspace is not null)
                AllOrders = await _work.GetRestaurantOrdersAsync(Workspace.Restaurant.Id, ct);

            return Page();
        }

        public Task<IActionResult> OnPostServeAsync(long orderId, CancellationToken ct) =>
            RunAsync(ws => _orders.MarkServedAsync(ws.Restaurant.Id, orderId, ct), "Order marked as served.", ct);

        public Task<IActionResult> OnPostCancelAsync(long orderId, CancellationToken ct) =>
            RunAsync(ws => _orders.CancelOrderAsync(ws.Restaurant.Id, orderId, ct), "Order cancelled.", ct);
    }
}
