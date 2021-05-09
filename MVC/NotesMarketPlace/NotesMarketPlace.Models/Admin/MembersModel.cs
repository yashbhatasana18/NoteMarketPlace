using System;

namespace NotesMarketPlace.Models.Admin
{
    public class MembersModel
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string ProfileImage { get; set; }

        public string Email { get; set; }

        public DateTime? JoinDate { get; set; }

        public int UnderReviewNotes { get; set; }

        public int PublishedNotes { get; set; }

        public int DownloadedNotes { get; set; }

        public decimal? TotalExpense { get; set; }

        public decimal? TotalEarning { get; set; }

        // member details

        public DateTime? DOB { get; set; }

        public string Phone { get; set; }

        public string University { get; set; }

        public string Address1 { get; set; }

        public string Address2 { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Country { get; set; }

        public string Zipcode { get; set; }
    }

    public class MemberNoteModel
    {
        public int Id { get; set; }

        public int NoteId { get; set; }

        public string Title { get; set; }

        public string Category { get; set; }

        public string Status { get; set; }

        public int DownloadedNote { get; set; }

        public decimal? Earning { get; set; }

        public DateTime? DateAdded { get; set; }

        public DateTime? PublishedDate { get; set; }
    }
}
