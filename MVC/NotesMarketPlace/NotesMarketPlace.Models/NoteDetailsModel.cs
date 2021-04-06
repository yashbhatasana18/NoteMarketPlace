using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotesMarketPlace.Models
{
    public class NoteDetailsModel
    {

        public int ID { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public decimal? Price { get; set; }
        public string Institute { get; set; }
        public string Country { get; set; }
        public string CourseName { get; set; }
        public string CourseCode { get; set; }
        public string Professor { get; set; }
        public decimal Pages { get; set; }

        public DateTime? ApprovedDate { get; set; }
        public string ApproveDate { get; set; }

        public string NotePreview { get; set; }
        public string Seller { get; set; }

        public int Status { get; set; }
    }

    public class CustomerReview
    {

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Image { get; set; }
        public decimal? Ratings { get; set; }
        public string Review { get; set; }

    }
}
