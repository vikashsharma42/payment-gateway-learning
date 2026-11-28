using DineBook.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace DineBook.Web.Dtos
{
    public class CustomerProfileDto
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100)]
        [Display(Name = "First name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100)]
        [Display(Name = "Last name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Choose your country.")]
        [Display(Name = "Country")]
        public Country CountryCode { get; set; } = CountryRules.Default;

        [Required(ErrorMessage = "Phone is required.")]
        [RegularExpression(@"^\+?[0-9 ]{4,20}$", ErrorMessage = "Enter a valid phone number.")]
        [Display(Name = "Phone")]
        public string Phone { get; set; }
    }

}
