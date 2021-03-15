using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotesMarketPlace.Models
{
    public class UserProfileModel
    {
        public int UserProfileID { get; set; }
        public int UserID { get; set; }
        public DateTime? DOB { get; set; }
        public int? Gender { get; set; }
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
    }
}
