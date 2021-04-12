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
        public string FirstName { get; set; }

        [Required(ErrorMessage = "LastName Is Required Field")]
        public string LastName { get; set; }

        public string Email { get; set; }

        public string SecondaryEmail { get; set; }

        public string Phonecode { get; set; }

        public string Phone { get; set; }

        public string ProfileImage { get; set; }

        public HttpPostedFileBase UserProfilePicturePath { get; set; }

        public CountriesModel PhoneCodeModel { get; set; }
    }
}
