using DineBook.Application.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Manager
{
    // Branches of the restaurant: add, open or close, default location, and each location's web address
    public class LocationsModel : ManagerPageModel
    {
        public LocationsModel(IStaffLocationService staffLocations, IRestaurantAdminService admin)
            : base(staffLocations, admin)
        {
        }

        [BindProperty]
        public NewLocationInput NewLocation { get; set; } = new();

        public class NewLocationInput
        {
            [Required(ErrorMessage = "Location name is required.")]
            [StringLength(100)]
            [Display(Name = "Name")]
            public string Name { get; set; } = string.Empty;

            [Required(ErrorMessage = "Street is required.")]
            [StringLength(200)]
            [Display(Name = "Street")]
            public string Street { get; set; } = string.Empty;

            [Required(ErrorMessage = "City is required.")]
            [StringLength(100)]
            [Display(Name = "City")]
            public string City { get; set; } = string.Empty;

            [Required(ErrorMessage = "State is required.")]
            [StringLength(100)]
            [Display(Name = "State")]
            public string State { get; set; } = string.Empty;

            [Required(ErrorMessage = "Country is required.")]
            [StringLength(100)]
            [Display(Name = "Country")]
            public string Country { get; set; } = "India";

            [Required(ErrorMessage = "Zip code is required.")]
            [StringLength(20)]
            [Display(Name = "Zip code")]
            public string ZipCode { get; set; } = string.Empty;

            [StringLength(15)]
            [Display(Name = "Phone (optional)")]
            public string Phone { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            await LoadWorkspaceAsync(null, ct);
            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync(CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadWorkspaceAsync(null, ct);
                ToastError("Check the location details and try again.");
                return Page();
            }

            var input = NewLocation;
            return await RunAsync(
                ws => Admin.AddLocationAsync(ws.Restaurant.Id, new AddLocationRequest(
                    input.Name, input.Street, input.City, input.State, input.Country, input.ZipCode, input.Phone), ct),
                "Location added. Add its tables next.",
                ct);
        }

        public Task<IActionResult> OnPostToggleAsync(long locationId, bool active, CancellationToken ct) =>
            RunAsync(
                ws => Admin.SetLocationActiveAsync(ws.Restaurant.Id, locationId, active, ct),
                active ? "Location reopened." : "Location closed for staff and new bookings.",
                ct);

        public Task<IActionResult> OnPostDefaultAsync(long locationId, CancellationToken ct) =>
            RunAsync(
                ws => Admin.SetDefaultLocationAsync(ws.Restaurant.Id, locationId, ct),
                "Default location changed.",
                ct);

        public Task<IActionResult> OnPostSlugAsync(long locationId, string slug, CancellationToken ct) =>
            RunAsync(
                ws => Admin.ChangeLocationSlugAsync(ws.Restaurant.Id, locationId, (slug ?? string.Empty).Trim().ToLowerInvariant(), ct),
                "Web address saved.",
                ct);
    }
}
