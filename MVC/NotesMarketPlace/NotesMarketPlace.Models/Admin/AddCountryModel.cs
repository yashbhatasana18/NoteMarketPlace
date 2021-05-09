using System.ComponentModel.DataAnnotations;

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
