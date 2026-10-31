using System.ComponentModel.DataAnnotations;

namespace ProductShop.API.DataObjects.Request
{
    public class CreateOrderRequest
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Required]
        [EmailAddress]
        public string CustomerEmail { get; set; }

        [Required]
        [StringLength(15, MinimumLength = 10)]
        public string CustomerPhone { get; set; }
    }
}