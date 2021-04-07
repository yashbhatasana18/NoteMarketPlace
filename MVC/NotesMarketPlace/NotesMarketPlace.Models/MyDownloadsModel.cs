using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotesMarketPlace.Models
{
    public class MyDownloadsModel
    {
        public int NoteId { get; set; }

        public int PurchaseId { get; set; }

        public int UserID { get; set; }

        public string Title { get; set; }

        public string Category { get; set; }

        public string EmailID { get; set; }

        public string Phone { get; set; }

        public string Buyer { get; set; }

        public string SellType { get; set; }

        public decimal? Price { get; set; }

        public DateTime? DownloadDate { get; set; }

        public DownloadsModel mydownloadtbl { get; set; }
        public SignUpModel userstbl { get; set; }
    }
}
