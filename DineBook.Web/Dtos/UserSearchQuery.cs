using System.ComponentModel.DataAnnotations;

namespace DineBook.Web.Dtos
{
    // Search box on the users list (?Search=...)
    public class UserSearchQuery : PageQuery
    {
        [Display(Name = "Search")]
        public string Search { get; set; }
    }
}
