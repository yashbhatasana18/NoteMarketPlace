using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotesMarketPlace.Models.Admin
{
    public class NotesUnderReviewModel
    {
        public int NoteId { get; set; }

        public string Title { get; set; }

        public string Category { get; set; }

        public int SellerId { get; set; }

        public string Seller { get; set; }

        public string status { get; set; }

        public DateTime? CreatedDate { get; set; }
    }
    public class SellerModel
    {
        public int SellerId { get; set; }

        public string SellerName { get; set; }
    }
}
