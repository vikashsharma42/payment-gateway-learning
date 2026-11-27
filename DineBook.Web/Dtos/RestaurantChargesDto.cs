using System.ComponentModel.DataAnnotations;

namespace DineBook.Web.Dtos
{
public class RestaurantChargesDto
    {
        [Range(0, 100, ErrorMessage = "GST must be between 0 and 100 percent.")]
        [Display(Name = "GST (percent)")]
        public decimal TaxPercent { get; set; }

        [Range(0, 10000, ErrorMessage = "The access fee must be between 0 and 10,000.")]
        [Display(Name = "System access fee per order")]
        public decimal SystemAccessFee { get; set; }
    }

}
