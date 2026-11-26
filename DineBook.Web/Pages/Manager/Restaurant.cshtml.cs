using DineBook.Application.Services.Interface;
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
        public DetailsInput Input { get; set; } = new();

        public class DetailsInput
        {
            [Required(ErrorMessage = "Phone is required.")]
            [StringLength(15)]
            [Display(Name = "Phone")]
            public string Phone { get; set; } = string.Empty;

            [StringLength(500)]
            [Display(Name = "Description")]
            public string Description { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            await LoadWorkspaceAsync(null, ct);
            if (Workspace is not null)
            {
                Input = new DetailsInput
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
                ToastError("Check the details and try again.");
                return Page();
            }

            return await RunAsync(
                ws => Admin.UpdateDetailsAsync(ws.Restaurant.Id, Input.Phone, Input.Description, ct),
                "Restaurant details saved.",
                ct);
        }
    }
}
