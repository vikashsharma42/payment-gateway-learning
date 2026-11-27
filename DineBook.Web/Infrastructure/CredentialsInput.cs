using System.ComponentModel.DataAnnotations;

namespace DineBook.Web.Infrastructure
{
    // The email and password fields of every sign-in form. Sign-in pages bind to this one class,
    // so the rules and the form fields are defined in a single place.
    public class CredentialsInput
    {
        [Required(ErrorMessage = "Enter your email.")]
        [EmailAddress(ErrorMessage = "Enter a valid email.")]
        [StringLength(254)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Enter your password.")]
        [StringLength(128)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
    }
}
