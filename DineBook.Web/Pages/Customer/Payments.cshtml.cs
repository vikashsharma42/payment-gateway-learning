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
    public class PaymentsModel : AppPageModel
    {
        private readonly ICustomerService _customers;

        public PaymentsModel(ICustomerService customers)
        {
            _customers = customers;
        }

        // Payments and refunds for the customer's orders
        public IReadOnlyList<Payment> Payments { get; private set; } = Array.Empty<Payment>();

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            Payments = await _customers.GetPaymentsAsync(UserId, ct);
            return Page();
        }
    }
}
