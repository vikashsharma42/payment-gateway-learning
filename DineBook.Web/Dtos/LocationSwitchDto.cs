using System.ComponentModel.DataAnnotations;

namespace DineBook.Web.Dtos
{
    // The location chosen in the staff location switcher (Staff/Index and Staff/Dashboard)
    public class LocationSwitchDto
    {
        public long LocationId { get; set; }
    }
}
