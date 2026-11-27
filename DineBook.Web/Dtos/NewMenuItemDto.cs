using System.ComponentModel.DataAnnotations;

namespace DineBook.Web.Dtos
{
    // Add-menu-item form on the restaurant admin page
    public class NewMenuItemDto
    {
        [Required(ErrorMessage = "Item name is required.")]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string ItemName { get; set; }

        [StringLength(300)]
        [Display(Name = "Description")]
        public string ItemDescription { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [StringLength(50)]
        [Display(Name = "Category")]
        public string ItemCategory { get; set; }

        [Range(0.01, 100000, ErrorMessage = "Price must be between 0.01 and 100000.")]
        [Display(Name = "Price")]
        public decimal ItemPrice { get; set; }
    }
}
