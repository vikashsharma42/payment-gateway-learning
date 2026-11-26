using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Web.Infrastructure;

namespace DineBook.Web.Pages.Customer
{
    public class OrdersModel : AppPageModel
    {
        private readonly ICustomerService _customers;

        public OrdersModel(ICustomerService customers)
        {
            _customers = customers;
        }

        public IReadOnlyList<Orders> Orders { get; private set; } = Array.Empty<Orders>();

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            Orders = await _customers.GetOrdersAsync(UserId, ct);
            return Page();
        }
    }
}
