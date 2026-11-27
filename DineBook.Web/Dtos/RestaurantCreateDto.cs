using System;
using System.ComponentModel.DataAnnotations;

namespace DineBook.Web.Dtos
{
    // Form fields for a new restaurant and its first location (Admin/Restaurants/New)
    public class RestaurantCreateDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Location name is required.")]
        [StringLength(100)]
        public string LocationName { get; set; }

        [Required(ErrorMessage = "Street is required.")]
        [StringLength(200)]
        public string Street { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100)]
        public string City { get; set; }

        [Required(ErrorMessage = "State is required.")]
        [StringLength(100)]
        public string State { get; set; }

        [Required(ErrorMessage = "Country is required.")]
        [StringLength(100)]
        public string Country { get; set; } = "India";

        [Required(ErrorMessage = "Zip code is required.")]
        [StringLength(20)]
        public string ZipCode { get; set; }

        [Required(ErrorMessage = "Opening time is required.")]
        [DataType(DataType.Time)]
        public TimeOnly OpensAt { get; set; } = new TimeOnly(12, 0);

        [Required(ErrorMessage = "Closing time is required.")]
        [DataType(DataType.Time)]
        public TimeOnly ClosesAt { get; set; } = new TimeOnly(23, 0);
    }
}
