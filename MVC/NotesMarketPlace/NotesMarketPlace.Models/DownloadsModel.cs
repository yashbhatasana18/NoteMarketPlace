using System;

namespace NotesMarketPlace.Models
{
    public class DownloadsModel
    {
        public int DownloadsID { get; set; }
        public int NoteID { get; set; }
        public int Seller { get; set; }
        public int Downloader { get; set; }
        public bool IsSellerHasAllowedDownload { get; set; }
        public string AttachmentPath { get; set; }
        public bool IsAttachmentDownloaded { get; set; }
        public DateTime? AttachmentDownloadedDate { get; set; }
        public bool IsPaid { get; set; }
        public decimal? PurchasedPrice { get; set; }
        public string NoteTitle { get; set; }
        public string NoteCategory { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
    }
}
