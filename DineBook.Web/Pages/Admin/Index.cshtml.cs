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
    public class IndexModel : AppPageModel
    {
        private readonly IReservationService _reservations;

        public IndexModel(IReservationService reservations)
        {
            _reservations = reservations;
        }

        // Every restaurant, open or closed, one page at a time
        public PagedResult<Restaurant> Restaurants { get; private set; }

        public async Task OnGetAsync([FromQuery] PageQuery query, CancellationToken ct)
        {
            Restaurants = await _reservations.GetRestaurantsPageAsync(query.Page, query.PageSize, openOnly: false, ct);
        }
    }
}
