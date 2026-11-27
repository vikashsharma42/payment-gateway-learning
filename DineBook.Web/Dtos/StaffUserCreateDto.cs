using DineBook.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace DineBook.Web.Dtos
{
    public class StaffUserCreateDto
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100)]
        [Display(Name = "First name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100)]
        [Display(Name = "Last name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email.")]
        [StringLength(254)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(128, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Temporary password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Choose a role.")]
        [Display(Name = "Role")]
        public UserRole Role { get; set; } = UserRole.Staff;

        // Required for Staff and Manager. Admins are not tied to a restaurant.
        [Display(Name = "Restaurant")]
        public long? RestaurantId { get; set; }
    }

}
