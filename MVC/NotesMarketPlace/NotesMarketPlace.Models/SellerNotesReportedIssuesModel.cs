using System;

namespace NotesMarketPlace.Models
{
    public class SellerNotesReportedIssuesModel
    {
        public int SellerNotesReportedIssuesID { get; set; }
        public int NoteID { get; set; }
        public int ReportedByID { get; set; }
        public int AgainstDownloadID { get; set; }
        public string Remarks { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }

        public int Total { get; set; }
    }
}
