using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotesMarketPlace.Models
{
    public class SearchNotesModel
    {
        public int SellerNotesID { get; set; }
        public int SellerID { get; set; }
        public int Status { get; set; }

        public int? ActionedBy { get; set; }

        public string AdminRemarks { get; set; }

        public DateTime? PublishedDate { get; set; }

        public string Title { get; set; }

        public int Category { get; set; }

        public string DisplayPicture { get; set; }

        public int? NoteType { get; set; }

        public int? NumberOfPages { get; set; }

        public string Description { get; set; }

        public string UniversityName { get; set; }

        public int? Country { get; set; }

        public string Course { get; set; }
        public string CourseCode { get; set; }
        public string Professor { get; set; }

        public bool IsPaid { get; set; }
        public decimal? SellingPrice { get; set; }
        public string NotesPreview { get; set; }

        public decimal? Reviews { get; set; }
        public int TotalReviews { get; set; }
        public int TotalSpams { get; set; }

        public SellerNotesReviewsModel SellerNotesReviewsModel { get; set; }

    }
}
