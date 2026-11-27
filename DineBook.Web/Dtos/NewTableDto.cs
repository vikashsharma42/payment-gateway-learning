using System.ComponentModel.DataAnnotations;

namespace DineBook.Web.Dtos
{
    // Add-table form on the restaurant admin page
    public class NewTableDto
    {
        [Required(ErrorMessage = "Table label is required.")]
        [StringLength(20)]
        [Display(Name = "Table label")]
        public string TableLabel { get; set; }

        [Range(1, 20, ErrorMessage = "Capacity must be between 1 and 20.")]
        [Display(Name = "Seats")]
        public int TableCapacity { get; set; } = 2;
    }
}
