using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotesMarketPlace.Models
{
    public class DashboardModel
    {
        public IEnumerable<SellerNotesModel> InProgressNote { get; set; }
        public IEnumerable<SellerNotesModel> PublishedNote { get; set; }

        public int MyDownload { get; set; }
        public int NumberOfSoldNote { get; set; }
        public int MoneyEarned { get; set; }
        public int MyRejectedNote { get; set; }
        public int BuyerRequest { get; set; }
    }
}
