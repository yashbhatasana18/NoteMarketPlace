using System;

namespace NotesMarketPlace.Models.Admin
{
    public class DownloadedNotesModel
    {
        public int NoteId { get; set; }

        public string Title { get; set; }

        public string Category { get; set; }

        public int BuyerId { get; set; }

        public string BuyerName { get; set; }

        public int SellerId { get; set; }

        public string SellerName { get; set; }

        public decimal? Price { get; set; }

        public bool IsPaid { get; set; }

        public DateTime DownloadedDate { get; set; }
    }
    public class BuyerModel
    {
        public int BuyerId { get; set; }

        public string BuyerName { get; set; }
    }
    public class NoteModel
    {
        public int NoteId { get; set; }

        public string NoteTitle { get; set; }
    }
}
