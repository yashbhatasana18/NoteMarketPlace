using System;

namespace NotesMarketPlace.Models
{
    public class SellerNotesAttachementsModel
    {
        public int SellerNotesAttachementsID { get; set; }
        public int NoteID { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
    }
}
