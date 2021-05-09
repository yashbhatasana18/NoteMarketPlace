using System;

namespace NotesMarketPlace.Models
{
    public class BuyerRequestModel
    {
        public int DownloadsID { get; set; }

        public int NoteID { get; set; }

        public int Seller { get; set; }

        public string Downloader { get; set; }

        public bool IsSellerHasAllowedDownload { get; set; }

        public string AttachmentPath { get; set; }

        public bool IsAttachmentDownloaded { get; set; }

        public System.DateTime? AttachmentDownloadedDate { get; set; }

        public string IsPaid { get; set; }

        public decimal? PurchasedPrice { get; set; }

        public string NoteTitle { get; set; }

        public string NoteCategory { get; set; }

        public string Phone { get; set; }

        public DateTime? CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
    }
}
