using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotesMarketPlace.Models.Admin
{
    public class RejectedNotesModel
    {
        public int NoteId { get; set; }

        public string Title { get; set; }

        public string Category { get; set; }

        public int SellerId { get; set; }

        public string SellerName { get; set; }

        public DateTime? PublishedDate { get; set; }

        public string RejectedBy { get; set; }

        public string Remarks { get; set; }

        public bool IsPaid { get; set; }
}
}
