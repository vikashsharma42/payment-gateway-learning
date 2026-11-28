using DineBook.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace DineBook.Web.Dtos
{
    public class CustomerRegistrationDto
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

        [Required(ErrorMessage = "Choose your country.")]
        [Display(Name = "Country")]
        public Country CountryCode { get; set; } = CountryRules.Default;

        [Required(ErrorMessage = "Phone is required.")]
        [RegularExpression(@"^\+?[0-9 ]{4,20}$", ErrorMessage = "Enter a valid phone number.")]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(128, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; }
    }

}
