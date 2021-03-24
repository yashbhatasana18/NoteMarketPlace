using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace NotesMarketPlace.Models
{
    public class UserProfileModel
    {
        public int UserProfileID { get; set; }
        public int UserID { get; set; }

        [DisplayName("First Name")]
        [Required(ErrorMessage = "First name is required")]
        [MaxLength(50, ErrorMessage = "Length should be <50")]
        public string FirstName { get; set; }

        [DisplayName("Last Name")]
        [Required(ErrorMessage = "Last name is required")]
        [MaxLength(50, ErrorMessage = "Length should be <50")]
        public string LastName { get; set; }

        [DisplayName("Email Address")]
        [Required(ErrorMessage = "Email address is required")]
        [MaxLength(100, ErrorMessage = "Length should be <100")]
        [DataType(DataType.EmailAddress, ErrorMessage = "Use valid email address")]
        [EmailAddress]
        public string EmailID { get; set; }

        public DateTime? DOB { get; set; }
        public int? Gender { get; set; }

        [DisplayName("Secondary Email Address")]
        [MaxLength(100, ErrorMessage = "Length should be <100")]
        [DataType(DataType.EmailAddress, ErrorMessage = "Use valid email address")]
        [EmailAddress]
        public string SecondaryEmailAddress { get; set; }
        public string PhoneNumberCountryCode { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfilePicture { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string Country { get; set; }
        public string University { get; set; }
        public string College { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }

        public HttpPostedFileBase UserProfilePicturePath { get; set; }

        public ReferenceDataModel genderModel { get; set; }

        public CountriesModel countryModel { get; set; }

        public CountriesModel CountryCodeModel { get; set; }
    }
}
