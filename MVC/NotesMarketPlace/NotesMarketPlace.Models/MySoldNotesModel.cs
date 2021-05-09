using System;

namespace NotesMarketPlace.Models
{
    public class MySoldNotesModel
    {
        public int Id { get; set; }

        public int NoteId { get; set; }

        public int UserID { get; set; }

        public string EmailID { get; set; }

        public string Phone { get; set; }

        public string Title { get; set; }

        public string Category { get; set; }

        public string Buyer { get; set; }

        public string SellType { get; set; }

        public decimal? Price { get; set; }

        public DateTime? DownloadDate { get; set; }
    }
}
