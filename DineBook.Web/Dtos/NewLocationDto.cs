using System.ComponentModel.DataAnnotations;

namespace DineBook.Web.Dtos
{
    public class NewLocationDto
    {
        [Required(ErrorMessage = "Location name is required.")]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Street is required.")]
        [StringLength(200)]
        [Display(Name = "Street")]
        public string Street { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100)]
        [Display(Name = "City")]
        public string City { get; set; }

        [Required(ErrorMessage = "State is required.")]
        [StringLength(100)]
        [Display(Name = "State")]
        public string State { get; set; }

        [Required(ErrorMessage = "Country is required.")]
        [StringLength(100)]
        [Display(Name = "Country")]
        public string Country { get; set; } = "India";

        [Required(ErrorMessage = "Zip code is required.")]
        [StringLength(20)]
        [Display(Name = "Zip code")]
        public string ZipCode { get; set; }

        [StringLength(15)]
        [Display(Name = "Phone (optional)")]
        public string Phone { get; set; }
    }
}
