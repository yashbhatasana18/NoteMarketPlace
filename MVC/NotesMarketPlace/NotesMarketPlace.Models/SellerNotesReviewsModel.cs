using System;

namespace NotesMarketPlace.Models
{
    public class SellerNotesReviewsModel
    {
        public int SellerNotesReviewsID { get; set; }
        public int NoteID { get; set; }
        public int ReviewedByID { get; set; }
        public int AgainstDownloadID { get; set; }
        public decimal Ratings { get; set; }
        public string Comments { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsActive { get; set; }

        public int Total { get; set; }
    }
}
