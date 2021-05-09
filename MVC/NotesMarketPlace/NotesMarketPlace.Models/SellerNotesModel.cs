using System;

namespace NotesMarketPlace.Models
{
    public class SellerNotesModel
    {
        public int SellerNotesID { get; set; }
        public int SellerID { get; set; }
        public int Status { get; set; }
        public string StatusValue { get; set; }
        public int? ActionedBy { get; set; }
        public string AdminRemarks { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string Title { get; set; }
        public int Category { get; set; }
        public string CategoryName { get; set; }
        public string DisplayPicture { get; set; }
        public int? NoteType { get; set; }
        public int? NumberOfPages { get; set; }
        public string Description { get; set; }
        public string UniversityName { get; set; }
        public int? Country { get; set; }
        public string Course { get; set; }
        public string CourseCode { get; set; }
        public string Professor { get; set; }
        public bool IsPaid { get; set; }
        public decimal? SellingPrice { get; set; }
        public string NotesPreview { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsActive { get; set; }

        public NoteCategoriesModel NoteCategoriesModel { get; set; }

        public ReferenceDataModel ReferenceDataModel { get; set; }
    }
}
