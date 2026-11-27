using DineBook.Application.Services.Interface;
using DineBook.Web.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Manager.Restaurant
{
    // Tax and access fee used for the bills of this restaurant (Restaurant.UpdateCharges in the Domain)
    public class ChargesModel : ManagerPageModel
    {
        public ChargesModel(IStaffLocationService staffLocations, IRestaurantAdminService admin)
            : base(staffLocations, admin)
        {
        }

        [BindProperty]
        public RestaurantChargesDto Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            await LoadWorkspaceAsync(null, ct);
            if (Workspace is not null)
            {
                Input = new RestaurantChargesDto
                {
                    TaxPercent = Workspace.Restaurant.TaxPercent,
                    SystemAccessFee = Workspace.Restaurant.SystemAccessFee
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadWorkspaceAsync(null, ct);
                ToastError("Please check the rates and try again.");
                return Page();
            }

            return await RunAsync(
                ws => Admin.UpdateChargesAsync(ws.Restaurant.Id, Input.TaxPercent, Input.SystemAccessFee, ct),
                "Charges saved. New bills use these rates.",
                ct);
        }
    }
}
