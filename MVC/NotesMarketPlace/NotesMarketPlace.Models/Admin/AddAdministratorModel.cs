using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotesMarketPlace.Models.Admin
{
    public class AddAdministratorModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "* First Name is required")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "* Last Name is required")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "* Email address is required")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "required")]
        public string CountryCode { get; set; }

        [Required(ErrorMessage = "* Phone is required")]
        public string Phone { get; set; }

        public List<CountriesModel> CountryModel { get; set; }
    }
}
