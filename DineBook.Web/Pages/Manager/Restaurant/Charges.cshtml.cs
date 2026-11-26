using DineBook.Application.Services.Interface;
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
        public ChargesInput Input { get; set; } = new();

        public class ChargesInput
        {
            [Range(0, 100, ErrorMessage = "GST must be between 0 and 100 percent.")]
            [Display(Name = "GST (percent)")]
            public decimal TaxPercent { get; set; }

            [Range(0, 10000, ErrorMessage = "The access fee must be between 0 and 10,000.")]
            [Display(Name = "System access fee per order")]
            public decimal SystemAccessFee { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            await LoadWorkspaceAsync(null, ct);
            if (Workspace is not null)
            {
                Input = new ChargesInput
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
                ToastError("Check the rates and try again.");
                return Page();
            }

            return await RunAsync(
                ws => Admin.UpdateChargesAsync(ws.Restaurant.Id, Input.TaxPercent, Input.SystemAccessFee, ct),
                "Charges saved. New bills use these rates.",
                ct);
        }
    }
}
