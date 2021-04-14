using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotesMarketPlace.Models.Admin
{
    public class AddCountryModel
    {
        public int Id { get; set; }

        [Required]
        public string CountryName { get; set; }

        [Required]
        public string CountryCode { get; set; }
    }
}
