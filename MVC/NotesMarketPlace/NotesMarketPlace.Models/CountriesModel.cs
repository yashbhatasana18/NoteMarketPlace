using System;

namespace NotesMarketPlace.Models
{
    public class CountriesModel
    {
        public int CountriesID { get; set; }
        public string Name { get; set; }
        public string CountryCode { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
    }
}
