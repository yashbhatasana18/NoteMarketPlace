using System;
using System.Collections.Generic;

namespace NotesMarketPlace.Models
{
    public class DashboardModel
    {
        public IEnumerable<SellerNotesModel> InProgressNote { get; set; }
        public IEnumerable<SellerNotesModel> PublishedNote { get; set; }

        public class UserDashboardInProgressModel
        {

            public int Id { get; set; }

            public string Title { get; set; }

            public string Category { get; set; }

            public string Status { get; set; }

            public DateTime? AddedDate { get; set; }

        }

        public class UserDashboardPublishedNoteModel
        {
            public int Id { get; set; }

            public string Title { get; set; }

            public string Category { get; set; }

            public string SellType { get; set; }

            public decimal? Price { get; set; }

            public DateTime? AddedDate { get; set; }
        }

        public int MyDownload { get; set; }
        public int NumberOfSoldNote { get; set; }
        public int MoneyEarned { get; set; }
        public int MyRejectedNote { get; set; }
        public int BuyerRequest { get; set; }
    }
}
