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
    public class ReservationsModel : AppPageModel
    {
        private readonly ICustomerService _customers;

        public ReservationsModel(ICustomerService customers)
        {
            _customers = customers;
        }

        public IReadOnlyList<Reservation> Bookings { get; private set; } = Array.Empty<Reservation>();

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            Bookings = await _customers.GetReservationsAsync(UserId, ct);
            return Page();
        }
    }
}
