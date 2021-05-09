using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NotesMarketPlace.Models
{
    public class ContactUsModel
    {
        [DisplayName("Full Name")]
        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(50, ErrorMessage = "Length should be <50")]
        public string FullName { get; set; }

        [DisplayName("Email Address")]
        [Required(ErrorMessage = "Email address is required")]
        [MaxLength(100, ErrorMessage = "Length should be <100")]
        [DataType(DataType.EmailAddress, ErrorMessage = "Use valid email address")]
        [EmailAddress]
        public string EmailID { get; set; }

        [DisplayName("Subject")]
        [Required(ErrorMessage = "Subject is required")]
        public string Subject { get; set; }

        [DisplayName("Comments")]
        [Required(ErrorMessage = "Comments is required")]
        public string Comments { get; set; }
    }
}
