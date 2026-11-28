using DineBook.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace DineBook.Web.Dtos
{
    // The checkout form: who pays and the key that stops a double submit from paying twice
    public class CheckoutDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Choose your country.")]
        [Display(Name = "Country")]
        public Country CountryCode { get; set; } = CountryRules.Default;

        [Required(ErrorMessage = "Phone is required.")]
        [RegularExpression(@"^\+?[0-9 ]{4,20}$", ErrorMessage = "Enter a valid phone number.")]
        public string Phone { get; set; } = string.Empty;

        // One key per checkout form. A double submit or a "back" resubmit sends the same key,
        // so the customer gets the same payment instead of a new one.
        public string IdempotencyKey { get; set; } = string.Empty;
    }
}
