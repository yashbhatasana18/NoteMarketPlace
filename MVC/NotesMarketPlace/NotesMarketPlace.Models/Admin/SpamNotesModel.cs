using System;

namespace NotesMarketPlace.Models.Admin
{
    public class SpamNotesModel
    {
        public int ID { get; set; }

        public int NoteId { get; set; }

        public string ReportedBy { get; set; }

        public string Title { get; set; }

        public string Category { get; set; }

        public string Remarks { get; set; }

        public DateTime? DateAdded { get; set; }
    }
}
