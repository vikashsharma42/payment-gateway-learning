using DineBook.Domain.Entities;

namespace DineBook.Web.Models
{
    // What the shared restaurant header needs: the restaurant, the location being viewed,
    // and which tab is open (overview, menu or book). Used by the customer restaurant pages.
    public sealed record RestaurantHeader(Restaurant Restaurant, Location SelectedLocation, string ActiveTab);
}
