using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace NotesMarketPlace.Models.Admin
{
    public class MyProfile
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "FirstName Is Required Field")]
        [MaxLength(50, ErrorMessage = "Length should be <50")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "LastName Is Required Field")]
        [MaxLength(50, ErrorMessage = "Length should be <50")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email address is required")]
        [MaxLength(100, ErrorMessage = "Length should be <100")]
        [DataType(DataType.EmailAddress, ErrorMessage = "Use valid email address")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Secondary Email Is Required Field")]
        [MaxLength(100, ErrorMessage = "Length should be <100")]
        [DataType(DataType.EmailAddress, ErrorMessage = "Use valid secondary email address")]
        [EmailAddress]
        public string SecondaryEmail { get; set; }

        public string Phonecode { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        [StringLength(10, ErrorMessage = "Length should be = 10")]
        [DataType(DataType.PhoneNumber)]
        [RegularExpression(@"^\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$", ErrorMessage = "Not a valid phone number")]
        public string Phone { get; set; }

        public string ProfileImage { get; set; }

        public HttpPostedFileBase UserProfilePicturePath { get; set; }

        public CountriesModel PhoneCodeModel { get; set; }
    }
}
