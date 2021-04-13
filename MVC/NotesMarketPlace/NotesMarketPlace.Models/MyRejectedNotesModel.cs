using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotesMarketPlace.Models
{
    public class MyRejectedNotesModel
    {
        public int Id { get; set; }

        public int NoteId { get; set; }

        public string Title { get; set; }

        public string Category { get; set; }

        public string Remark { get; set; }

        public string DownloadNote { get; set; }

        public int UserId { get; set; }

        public DateTime? CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
    }
}
