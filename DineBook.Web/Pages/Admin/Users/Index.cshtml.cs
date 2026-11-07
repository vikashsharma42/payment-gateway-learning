using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;
using DineBook.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Admin.Users
{
    public class IndexModel : AppPageModel
    {
        private readonly IRegistrationService _registration;

        public IndexModel(IRegistrationService registration)
        {
            _registration = registration;
        }

        public IReadOnlyList<ApplicationUser> Users { get; private set; }

        [BindProperty]
        public CreateInputModel Input { get; set; }

        // Set when the create popup has to be shown again with its errors
        public bool ShowCreateModal { get; private set; }

        public IEnumerable<SelectListItem> RoleOptions { get; } =
            new[] { UserRole.Staff, UserRole.Manager, UserRole.Admin }
                .Select(r => new SelectListItem(r.ToString(), r.ToString()));

        public class CreateInputModel
        {
            [Required(ErrorMessage = "First name is required.")]
            [StringLength(100)]
            [Display(Name = "First name")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Last name is required.")]
            [StringLength(100)]
            [Display(Name = "Last name")]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Enter a valid email.")]
            [StringLength(254)]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required.")]
            [StringLength(128, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters.")]
            [DataType(DataType.Password)]
            [Display(Name = "Temporary password")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Choose a role.")]
            [Display(Name = "Role")]
            public UserRole Role { get; set; } = UserRole.Staff;
        }

        public async Task OnGetAsync(CancellationToken ct)
        {
            await LoadAsync(ct);
        }

        // Live check while typing in the popup (GET, same as the sign-up page)
        public async Task<JsonResult> OnGetIsEmailAvailable(string email, CancellationToken ct)
        {
            return new JsonResult(new { available = await _registration.IsEmailAvailableAsync(email, ct) });
        }

        public async Task<IActionResult> OnPostCreateAsync(CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadAsync(ct);
                ShowCreateModal = true;
                return Page();
            }

            await _registration.CreateStaffUserAsync(new StaffUserRequest(
                Input.FirstName, Input.LastName, Input.Email, Input.Password, Input.Role), ct);

            ToastSuccess($"{Input.FirstName} {Input.LastName} was added as {Input.Role}.");
            return RedirectToPage();
            
        }

        private async Task LoadAsync(CancellationToken ct)
        {
            Users = await _registration.GetUsersAsync(ct);
        }
    }
}
