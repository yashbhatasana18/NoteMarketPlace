using System;

namespace NotesMarketPlace.Models.Admin
{
    public class ManageCountryModel
    {
        public int Id { get; set; }
        public string CountryName { get; set; }
        public string CountryCode { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public string IsActive { get; set; }
    }
}
