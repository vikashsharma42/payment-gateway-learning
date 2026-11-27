using System.ComponentModel.DataAnnotations;

namespace DineBook.Web.Dtos
{
    public class RestaurantDetailsDto
    {
        [Required(ErrorMessage = "Phone is required.")]
        [StringLength(15)]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; }
    }

}
