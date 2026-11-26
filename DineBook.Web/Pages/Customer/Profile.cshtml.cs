using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Exceptions;
using DineBook.Web.Infrastructure;

namespace DineBook.Web.Pages.Customer
{
    public class ProfileModel : AppPageModel
    {
        private readonly ICustomerService _customers;

        public ProfileModel(ICustomerService customers)
        {
            _customers = customers;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Shown but not editable here. Changing the sign-in email is not part of this version.
        public string Email { get; private set; } = string.Empty;

        public class InputModel
        {
            [Required(ErrorMessage = "First name is required.")]
            [StringLength(100)]
            [Display(Name = "First name")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Last name is required.")]
            [StringLength(100)]
            [Display(Name = "Last name")]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Phone is required.")]
            [RegularExpression(@"^\+?[0-9 ]{10,15}$", ErrorMessage = "Enter a valid phone number.")]
            [Display(Name = "Phone")]
            public string Phone { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            var profile = await _customers.GetProfileAsync(UserId, ct);
            Email = profile.Email;
            Input = new InputModel
            {
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Phone = profile.Phone ?? string.Empty
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadEmailAsync(ct);
                return Page();
            }

            try
            {
                await _customers.UpdateProfileAsync(UserId, Input.FirstName, Input.LastName, Input.Phone, ct);
                ToastSuccess("Your profile is saved.");
                return RedirectToPage();
            }
            catch (DomainException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadEmailAsync(ct);
                return Page();
            }
        }

        private async Task LoadEmailAsync(CancellationToken ct)
        {
            Email = (await _customers.GetProfileAsync(UserId, ct)).Email;
        }
    }
}
