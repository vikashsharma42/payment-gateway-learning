using DineBook.Application.Services.Interface;
using DineBook.Web.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Manager
{
    public class RestaurantModel : ManagerPageModel
    {
        public RestaurantModel(IStaffLocationService staffLocations, IRestaurantAdminService admin)
            : base(staffLocations, admin)
        {
        }

        [BindProperty]
        public RestaurantDetailsDto Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            await LoadWorkspaceAsync(null, ct);
            if (Workspace is not null)
            {
                Input = new RestaurantDetailsDto
                {
                    Phone = Workspace.Restaurant.Phone ?? string.Empty,
                    Description = Workspace.Restaurant.Description ?? string.Empty
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadWorkspaceAsync(null, ct);
                ToastError("Please check the details and try again.");
                return Page();
            }

            return await RunAsync(
                ws => Admin.UpdateDetailsAsync(ws.Restaurant.Id, Input.Phone, Input.Description, ct),
                "Restaurant details saved.",
                ct);
        }
    }
}
