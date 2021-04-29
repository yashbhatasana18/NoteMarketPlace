using NotesMarketPlace.DB;
using NotesMarketPlace.DB.DBOperations;
using NotesMarketPlace.Email;
using NotesMarketPlace.Models.Admin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;

namespace NotesMarketPlace.Controllers
{
    [Authorize(Roles = "Admin, Super Admin")]
    public class AdminController : Controller
    {
        readonly AdminRepository adminRepository = null;

        #region Default Constructor

        public AdminController()
        {
            adminRepository = new AdminRepository();

            using (var context = new NotesMarketPlaceEntities())
            {
                Models.SocialUrlModel socialUrlModel = new Models.SocialUrlModel();

                // social URL
                var socialUrl = context.SystemConfigurations.Where(m => m.Key == "Facebook" || m.Key == "Twitter" || m.Key == "Linkedin").ToList();

                socialUrlModel.Facebook = socialUrl[0].Value;
                socialUrlModel.Twitter = socialUrl[1].Value;
                socialUrlModel.Linkedin = socialUrl[2].Value;

                ViewBag.Facebook = socialUrlModel.Facebook;
                ViewBag.Twitter = socialUrlModel.Twitter;
                ViewBag.Linkedin = socialUrlModel.Linkedin;
            }
        }

        #endregion Default Constructor

        #region Initialize User Information

        protected override void Initialize(RequestContext requestContext)
        {
            base.Initialize(requestContext);
            if (requestContext.HttpContext.User.Identity.IsAuthenticated)
            {
                using (var context = new NotesMarketPlaceEntities())
                {
                    var currentUser = context.Users.FirstOrDefault(m => m.EmailID == User.Identity.Name);

                    var img = (from Details in context.UserProfile
                               join Users in context.Users on Details.UserID equals Users.UserID
                               where Users.EmailID == requestContext.HttpContext.User.Identity.Name
                               select Details.ProfilePicture).FirstOrDefault();

                    string fileName = System.IO.Path.GetFileName(img);

                    string filePath = "Members/" + currentUser.UserID + "/" + fileName;

                    if (img == null)
                    {
                        var defaultImg = context.SystemConfigurations.FirstOrDefault(m => m.Key == "DefaultProfileImage").Value;
                        ViewBag.UserProfile = defaultImg;
                    }
                    else
                    {
                        ViewBag.UserProfile = filePath;
                    }
                }
            }
        }

        #endregion Initialize User Information

        #region Deactivate User

        public void DeactivateUser(int userId)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var currentAdmin = context.Users.Single(m => m.EmailID == User.Identity.Name).UserID;

                var user = context.Users.Single(m => m.UserID == userId);

                user.IsActive = false;
                user.ModifiedBy = currentAdmin;
                user.ModifiedDate = DateTime.Now;
                context.SaveChanges();

                var notes = context.SellerNotes.Where(m => m.SellerID == userId).ToList();

                for (int i = 0; i < notes.Count; i++)
                {
                    var note = notes[i];

                    var Attachment = context.SellerNotesAttachements.Single(m => m.NoteID == note.SellerNotesID);
                    Attachment.IsActive = false;
                    Attachment.ModifiedBy = currentAdmin;
                    Attachment.ModifiedDate = DateTime.Now;

                    notes[i].IsActive = false;
                    notes[i].Status = 10;
                    notes[i].ActionedBy = currentAdmin;
                    notes[i].PublishedDate = DateTime.Now;

                    context.SaveChanges();
                }
            }
        }

        #endregion Deactivate User

        #region Dashboard

        public ActionResult Dashboard(int? month, string txtSearch, string SortOrder, string SortBy, int PageNumber = 1)
        {
            //if (Session["emailID"] == null)
            //{
            //    return RedirectToAction("Login", "Account");
            //}

            using (var context = new NotesMarketPlaceEntities())
            {
                //Total Notes In Review
                var inReviewNote = context.SellerNotes.Where(m => (m.Status == 7 || m.Status == 8) && m.IsActive == true).Count();
                //Total Notes Downloaded (last 7 days)
                var condition = DateTime.Now.Date.AddDays(-7);
                var downloads = context.Downloads.Where(m => m.IsSellerHasAllowedDownload == true && m.IsAttachmentDownloaded == true && m.AttachmentDownloadedDate >= condition).Count();
                //Total New Registration (last 7 days)
                var registration = context.Users.Where(m => m.CreatedDate >= condition).Count();

                ViewBag.InReview = inReviewNote;
                ViewBag.Downloads = downloads;
                ViewBag.Registration = registration;

                //Last 6 Month From Today
                var monthList = new List<MonthModel>();
                for (int i = 0; i <= 6; i++)
                {
                    monthList.Add(new MonthModel
                    {
                        digit = DateTime.Today.AddMonths(-i).Month,
                        Month = DateTime.Today.AddMonths(-i).ToString("MMMM")
                    });
                }
                ViewBag.MonthList = monthList;

                //Published Note Data
                var note = (from Note in context.SellerNotes
                            join Attachment in context.SellerNotesAttachements on Note.SellerNotesID equals Attachment.NoteID
                            join Category in context.NoteCategories on Note.Category equals Category.NoteCategoriesID
                            join User in context.Users on Note.SellerID equals User.UserID
                            where Note.Status == 9
                            let total = (context.Downloads.Where(m => m.NoteID == Note.SellerNotesID).Count())
                            select new DashboardModel
                            {
                                Id = Note.SellerNotesID,
                                Title = Note.Title,
                                Category = Category.Name,
                                Price = Note.SellingPrice,
                                Publisher = User.FirstName + " " + User.LastName,
                                PublishDate = (DateTime)Note.PublishedDate,
                                publishMonth = Note.PublishedDate.Value.Month,
                                TotalDownloads = total,
                                userid = Note.SellerID,
                                filename = Attachment.FilePath,
                                IsPaid = Note.IsPaid
                            }).OrderByDescending(x => x.TotalDownloads).ToList();

                ViewBag.SortOrder = SortOrder;
                ViewBag.SortBy = SortBy;
                var Dashboardresult = note;

                if (txtSearch != null)
                {
                    Dashboardresult = Dashboardresult.Where(x => x.Title.ToLower().Contains(txtSearch.ToLower())).ToList();

                    //Sorting
                    Dashboardresult = ApplySorting(SortOrder, SortBy, Dashboardresult);

                    //Pagination
                    Dashboardresult = ApplyPagination(Dashboardresult, PageNumber);
                }
                else
                {
                    //Sorting
                    Dashboardresult = ApplySorting(SortOrder, SortBy, Dashboardresult);

                    //Pagination
                    Dashboardresult = ApplyPagination(Dashboardresult, PageNumber);
                }

                //Attachment Size
                foreach (var data in Dashboardresult)
                {
                    data.AttachmentSize = GetSize(data.userid, data.Id, data.filename);
                }

                if (month == null)
                {
                    var filternote = Dashboardresult.Where(m => m.publishMonth == DateTime.Now.Month).ToList();
                    return View(filternote);
                }
                else
                {
                    var filternote = Dashboardresult.Where(m => m.publishMonth == month).ToList();
                    return View(filternote);
                }
            }
        }

        #endregion Dashboard

        #region Unpublish Note

        [HttpPost]
        public ActionResult Unpublishnote(int noteid, string Remarks)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                int currentAdmin = context.Users.Single(m => m.EmailID == User.Identity.Name).UserID;

                var note = context.SellerNotes.Single(m => m.SellerNotesID == noteid);

                var seller = context.Users.Single(m => m.UserID == note.SellerID);

                note.Status = 11;
                note.IsActive = false;
                note.AdminRemarks = Remarks;
                note.ActionedBy = currentAdmin;
                note.PublishedDate = DateTime.Now;
                note.ModifiedBy = currentAdmin;
                note.ModifiedDate = DateTime.Now;

                context.SaveChanges();

                string subject = "Sorry! We need to remove your notes from our portal.";
                string body = "Hello " + seller.FirstName + " " + seller.LastName + ",\n \n"
                    + "We want to inform you that, your note " + note.Title + " has been removed from the portal. Please find our remarks as below -\n";
                body += Remarks;
                body += "\n \nRegards,\nNotes Marketplace";

                bool isSend = SendEmailUser.EmailSend(seller.EmailID, subject, body, false);

                return RedirectToAction("Dashboard");
            }
        }

        #endregion Unpublish Note

        #region Notes Under Review

        public ActionResult NotesUnderReview(int? sellerId, string txtSearch, string SortOrder, string SortBy, int PageNumber = 1)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                //Seller Names
                var seller = (from Notes in context.SellerNotes
                              join User in context.Users on Notes.SellerID equals User.UserID
                              where Notes.Status == 6 || Notes.Status == 7 || Notes.Status == 8
                              group new { Notes, User } by Notes.SellerID into grp
                              select new SellerModel
                              {
                                  SellerId = grp.Select(x => x.User.UserID).FirstOrDefault(),
                                  SellerName = grp.Select(x => x.User.FirstName).FirstOrDefault() + " " + grp.Select(x => x.User.LastName).FirstOrDefault()
                              }).ToList();

                ViewBag.SellerList = seller;

                //Set Model Data
                var model = (from Notes in context.SellerNotes
                             join Status in context.ReferenceData on Notes.Status equals Status.ReferenceDataID
                             join Category in context.NoteCategories on Notes.Category equals Category.NoteCategoriesID
                             join User in context.Users on Notes.SellerID equals User.UserID
                             where Notes.Status == 6 || Notes.Status == 7 || Notes.Status == 8
                             select new NotesUnderReviewModel
                             {
                                 NoteId = Notes.SellerNotesID,
                                 Title = Notes.Title,
                                 Category = Category.Name,
                                 SellerId = Notes.SellerID,
                                 Seller = User.FirstName + " " + User.LastName,
                                 status = Status.Value,
                                 CreatedDate = Notes.CreatedDate
                             }).OrderByDescending(x => x.CreatedDate).ToList();

                ViewBag.SortOrder = SortOrder;
                ViewBag.SortBy = SortBy;
                var NotesUnderReviewresult = model;

                if (txtSearch != null)
                {
                    NotesUnderReviewresult = NotesUnderReviewresult.Where(x => x.Title.ToLower().Contains(txtSearch.ToLower())).ToList();

                    //Sorting
                    NotesUnderReviewresult = ApplySorting(SortOrder, SortBy, NotesUnderReviewresult);

                    //Pagination
                    NotesUnderReviewresult = ApplyPagination(NotesUnderReviewresult, PageNumber);
                }
                else
                {
                    //Sorting
                    NotesUnderReviewresult = ApplySorting(SortOrder, SortBy, NotesUnderReviewresult);

                    //Pagination
                    NotesUnderReviewresult = ApplyPagination(NotesUnderReviewresult, PageNumber);
                }

                if (sellerId == null)
                {
                    return View(NotesUnderReviewresult);
                }
                else
                {
                    var filtermodel = NotesUnderReviewresult.Where(m => m.SellerId == sellerId).ToList();
                    return View(filtermodel);
                }
            }
        }

        #region Change Note Status

        [HttpPost]
        public void NoteStatusUpdate(int noteid, string status)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var currentAdmin = context.Users.Single(m => m.EmailID == User.Identity.Name).UserID;

                var note = context.SellerNotes.Single(m => m.SellerNotesID == noteid);

                switch (status)
                {
                    case "InReview":
                        note.Status = 8;
                        break;
                    case "Approve":
                        note.Status = 9;
                        break;
                }

                note.ActionedBy = currentAdmin;
                note.PublishedDate = DateTime.Now;
                note.ModifiedBy = currentAdmin;
                note.ModifiedDate = DateTime.Now;

                context.SaveChanges();
            }
        }

        #endregion Change Note Status

        #region Reject Note

        [HttpPost]
        public ActionResult RejectNote(int noteId, string Reject)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var currentAdmin = context.Users.Single(m => m.EmailID == User.Identity.Name).UserID;

                var note = context.SellerNotes.Single(m => m.SellerNotesID == noteId);
                note.Status = 10;
                note.ActionedBy = currentAdmin;
                note.AdminRemarks = Reject;
                note.PublishedDate = DateTime.Now;
                note.ModifiedBy = currentAdmin;
                note.ModifiedDate = DateTime.Now;

                context.SaveChanges();

                return RedirectToAction("NotesUnderReview");
            }
        }

        #endregion Reject Note

        #endregion Notes Under Review

        #region Published Notes

        public ActionResult PublishedNotes(int? sellerId, string txtSearch, string SortOrder, string SortBy, int PageNumber = 1)
        {
            ViewBag.WhiteNav = "white-nav-top";
            using (var context = new NotesMarketPlaceEntities())
            {
                //Seller Names
                var seller = (from Notes in context.SellerNotes
                              join User in context.Users on Notes.SellerID equals User.UserID
                              where Notes.Status == 9
                              group new { Notes, User } by Notes.SellerID into grp
                              select new SellerModel
                              {
                                  SellerId = grp.Select(x => x.User.UserID).FirstOrDefault(),
                                  SellerName = grp.Select(x => x.User.FirstName).FirstOrDefault() + " " + grp.Select(x => x.User.LastName).FirstOrDefault()
                              }).ToList();

                ViewBag.SellerList = seller;

                //Set Model Data
                var model = (from Notes in context.SellerNotes
                             join User in context.Users on Notes.SellerID equals User.UserID
                             join Admin in context.Users on Notes.ActionedBy equals Admin.UserID
                             join Category in context.NoteCategories on Notes.Category equals Category.NoteCategoriesID
                             join Attachment in context.SellerNotesAttachements on Notes.SellerNotesID equals Attachment.NoteID
                             where Notes.Status == 9
                             let total = (context.Downloads.Where(m => m.NoteID == Notes.SellerNotesID && m.IsSellerHasAllowedDownload == true).Count())
                             select new PublishedNoteModel
                             {
                                 NoteId = Notes.SellerNotesID,
                                 Title = Notes.Title,
                                 Category = Category.Name,
                                 Price = Notes.SellingPrice,
                                 SellerId = Notes.SellerID,
                                 Seller = User.FirstName + " " + User.LastName,
                                 ApprovedBy = Admin.FirstName + " " + Admin.LastName,
                                 PublishDate = (DateTime)Notes.PublishedDate,
                                 TotalDownloads = total,
                                 IsPaid = Notes.IsPaid
                             }).OrderByDescending(x => x.PublishDate).ToList();

                ViewBag.SortOrder = SortOrder;
                ViewBag.SortBy = SortBy;
                var PublishedNotesresult = model;

                if (txtSearch != null)
                {
                    PublishedNotesresult = PublishedNotesresult.Where(x => x.Title.ToLower().Contains(txtSearch.ToLower())).ToList();

                    //Sorting
                    PublishedNotesresult = ApplySorting(SortOrder, SortBy, PublishedNotesresult);

                    //Pagination
                    PublishedNotesresult = ApplyPagination(PublishedNotesresult, PageNumber);
                }
                else
                {
                    //Sorting
                    PublishedNotesresult = ApplySorting(SortOrder, SortBy, PublishedNotesresult);

                    //Pagination
                    PublishedNotesresult = ApplyPagination(PublishedNotesresult, PageNumber);
                }

                if (sellerId == null)
                {
                    return View(PublishedNotesresult);
                }
                else
                {
                    var filtermodel = PublishedNotesresult.Where(m => m.SellerId == sellerId).ToList();
                    return View(filtermodel);
                }


            }
        }

        #endregion Published Notes

        #region Downloaded Notes

        public ActionResult DownloadedNotes(int? noteId, int? sellerId, int? buyerId, string txtSearch, string SortOrder, string SortBy, int PageNumber = 1)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                //Seller Names
                var seller = (from Notes in context.SellerNotes
                              join User in context.Users on Notes.SellerID equals User.UserID
                              where Notes.Status == 9
                              group new { Notes, User } by Notes.SellerID into grp
                              select new SellerModel
                              {
                                  SellerId = grp.Select(x => x.User.UserID).FirstOrDefault(),
                                  SellerName = grp.Select(x => x.User.FirstName).FirstOrDefault() + " " + grp.Select(x => x.User.LastName).FirstOrDefault()
                              }).ToList();

                ViewBag.SellerList = seller;

                //Buyer Names
                var buyer = (from Purchase in context.Downloads
                             join User in context.Users on Purchase.Downloader equals User.UserID
                             where Purchase.IsSellerHasAllowedDownload == true && Purchase.IsAttachmentDownloaded == true
                             group new { Purchase, User } by Purchase.Downloader into grp
                             select new BuyerModel
                             {
                                 BuyerId = grp.Select(x => x.User.UserID).FirstOrDefault(),
                                 BuyerName = grp.Select(x => x.User.FirstName).FirstOrDefault() + " " + grp.Select(x => x.User.LastName).FirstOrDefault()
                             }).ToList();

                ViewBag.BuyerList = buyer;

                //Notes Title
                var note = (from Purchase in context.Downloads
                            join Note in context.SellerNotes on Purchase.NoteID equals Note.SellerNotesID
                            where Purchase.IsSellerHasAllowedDownload == true && Purchase.IsAttachmentDownloaded == true
                            group new { Note, Purchase } by Note.SellerNotesID into grp
                            select new NoteModel
                            {
                                NoteId = grp.Select(x => x.Purchase.NoteID).FirstOrDefault(),
                                NoteTitle = grp.Select(x => x.Note.Title).FirstOrDefault()
                            }).ToList();

                ViewBag.NoteList = note;

                //Set Model Data
                var model = (from Purchase in context.Downloads
                             join Note in context.SellerNotes on Purchase.NoteID equals Note.SellerNotesID
                             join Downloader in context.Users on Purchase.Downloader equals Downloader.UserID
                             join Seller in context.Users on Purchase.Seller equals Seller.UserID
                             join Category in context.NoteCategories on Note.Category equals Category.NoteCategoriesID
                             where Purchase.IsSellerHasAllowedDownload == true && Purchase.IsAttachmentDownloaded == true
                             select new DownloadedNotesModel
                             {
                                 NoteId = Purchase.NoteID,
                                 Title = Note.Title,
                                 Category = Category.Name,
                                 Price = Purchase.PurchasedPrice,
                                 SellerId = Seller.UserID,
                                 BuyerId = Downloader.UserID,
                                 IsPaid = Note.IsPaid,
                                 SellerName = Seller.FirstName + " " + Seller.LastName,
                                 BuyerName = Downloader.FirstName + " " + Downloader.LastName,
                                 DownloadedDate = (DateTime)Purchase.AttachmentDownloadedDate
                             }).OrderByDescending(x => x.DownloadedDate).ToList();

                var filtermodel = model;

                if (!noteId.Equals(null))
                {
                    filtermodel = filtermodel.Where(m => m.NoteId == noteId).ToList();
                }
                if (!sellerId.Equals(null))
                {
                    filtermodel = filtermodel.Where(m => m.SellerId == sellerId).ToList();
                }
                if (!buyerId.Equals(null))
                {
                    filtermodel = filtermodel.Where(m => m.BuyerId == buyerId).ToList();
                }

                ViewBag.SortOrder = SortOrder;
                ViewBag.SortBy = SortBy;
                var DownloadedNotesresult = filtermodel;

                if (txtSearch != null)
                {
                    DownloadedNotesresult = DownloadedNotesresult.Where(x => x.Title.ToLower().Contains(txtSearch.ToLower())).ToList();

                    //Sorting
                    DownloadedNotesresult = ApplySorting(SortOrder, SortBy, DownloadedNotesresult);

                    //Pagination
                    DownloadedNotesresult = ApplyPagination(DownloadedNotesresult, PageNumber);
                }
                else
                {
                    //Sorting
                    DownloadedNotesresult = ApplySorting(SortOrder, SortBy, DownloadedNotesresult);

                    //Pagination
                    DownloadedNotesresult = ApplyPagination(DownloadedNotesresult, PageNumber);
                }

                return View(DownloadedNotesresult);
            }
        }

        #endregion Downloaded Notes

        #region Rejected Notes

        public ActionResult RejectedNotes(int? sellerId, string txtSearch, string SortOrder, string SortBy, int PageNumber = 1)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                //Seller Names
                var seller = (from Notes in context.SellerNotes
                              join User in context.Users on Notes.SellerID equals User.UserID
                              where Notes.Status == 10
                              group new { Notes, User } by Notes.SellerID into grp
                              select new SellerModel
                              {
                                  SellerId = grp.Select(x => x.User.UserID).FirstOrDefault(),
                                  SellerName = grp.Select(x => x.User.FirstName).FirstOrDefault() + " " + grp.Select(x => x.User.LastName).FirstOrDefault()
                              }).ToList();

                ViewBag.SellerList = seller;

                //Set Notes
                var notes = (from Note in context.SellerNotes
                             join Category in context.NoteCategories on Note.Category equals Category.NoteCategoriesID
                             join Seller in context.Users on Note.SellerID equals Seller.UserID
                             join Admin in context.Users on Note.ActionedBy equals Admin.UserID
                             where Note.Status == 10
                             select new RejectedNotesModel
                             {
                                 NoteId = Note.SellerNotesID,
                                 Title = Note.Title,
                                 Category = Category.Name,
                                 SellerId = Note.SellerID,
                                 IsPaid = Note.IsPaid,
                                 SellerName = Seller.FirstName + " " + Seller.LastName,
                                 RejectedBy = Admin.FirstName + " " + Admin.LastName,
                                 Remarks = Note.AdminRemarks,
                                 PublishedDate = (DateTime)Note.PublishedDate
                             }).OrderByDescending(x => x.PublishedDate).ToList();

                if (!sellerId.Equals(null))
                {
                    notes = notes.Where(x => x.SellerId == sellerId).ToList();
                }

                ViewBag.SortOrder = SortOrder;
                ViewBag.SortBy = SortBy;
                var RejectedNotesresult = notes;

                if (txtSearch != null)
                {
                    RejectedNotesresult = RejectedNotesresult.Where(x => x.Title.ToLower().Contains(txtSearch.ToLower())).ToList();

                    //Sorting
                    RejectedNotesresult = ApplySorting(SortOrder, SortBy, RejectedNotesresult);

                    //Pagination
                    RejectedNotesresult = ApplyPagination(RejectedNotesresult, PageNumber);
                }
                else
                {
                    //Sorting
                    RejectedNotesresult = ApplySorting(SortOrder, SortBy, RejectedNotesresult);

                    //Pagination
                    RejectedNotesresult = ApplyPagination(RejectedNotesresult, PageNumber);
                }

                return View(RejectedNotesresult);
            }
        }

        #endregion Rejected Notes

        #region Members

        public ActionResult Members(string txtSearch, string SortOrder, string SortBy, int PageNumber = 1)
        {
            using (var context = new NotesMarketPlaceEntities())
            {

                var model = (from User in context.Users
                             where User.RoleID == 3 && User.IsActive == true
                             let underReview = (from Notes in context.SellerNotes
                                                where Notes.SellerID == User.UserID && (Notes.Status == 7 || Notes.Status == 8)
                                                select Notes).Count()
                             let published = (from Notes in context.SellerNotes
                                              where Notes.SellerID == User.UserID && Notes.Status == 9
                                              select Notes).Count()
                             let downloaded = (from Purchase in context.Downloads
                                               where Purchase.Downloader == User.UserID && Purchase.IsAttachmentDownloaded == true
                                               select Purchase)
                             let sell = (from Purchase in context.Downloads
                                         where Purchase.Seller == User.UserID && Purchase.IsSellerHasAllowedDownload == true
                                         select Purchase)
                             select new MembersModel
                             {
                                 Id = User.UserID,
                                 FirstName = User.FirstName,
                                 LastName = User.LastName,
                                 Email = User.EmailID,
                                 JoinDate = User.CreatedDate,
                                 UnderReviewNotes = underReview,
                                 PublishedNotes = published,
                                 DownloadedNotes = downloaded.Count(),
                                 TotalExpense = downloaded.Sum(x => x.PurchasedPrice),
                                 TotalEarning = sell.Sum(x => x.PurchasedPrice)
                             }).OrderByDescending(x => x.JoinDate).ToList();

                ViewBag.SortOrder = SortOrder;
                ViewBag.SortBy = SortBy;
                var Membersresult = model;

                if (txtSearch != null)
                {
                    if (Membersresult.Any(x => x.FirstName.ToLower().Contains(txtSearch.ToLower())))
                    {
                        Membersresult = Membersresult.Where(x => x.FirstName.ToLower().Contains(txtSearch.ToLower())).ToList();
                    }
                    else if (Membersresult.Any(x => x.LastName.ToLower().Contains(txtSearch.ToLower())))
                    {
                        Membersresult = Membersresult.Where(x => x.LastName.ToLower().Contains(txtSearch.ToLower())).ToList();
                    }
                    else if (Membersresult.Any(x => x.Email.ToLower().Contains(txtSearch.ToLower())))
                    {
                        Membersresult = Membersresult.Where(x => x.Email.ToLower().Contains(txtSearch.ToLower())).ToList();
                    }
                    else
                    {
                        Membersresult = Membersresult.Where(x => x.FirstName.ToLower().Contains(txtSearch.ToLower())).ToList();
                    }

                    //Sorting
                    Membersresult = ApplySorting(SortOrder, SortBy, Membersresult);

                    //Pagination
                    Membersresult = ApplyPagination(Membersresult, PageNumber);
                }
                else
                {
                    //Sorting
                    Membersresult = ApplySorting(SortOrder, SortBy, Membersresult);

                    //Pagination
                    Membersresult = ApplyPagination(Membersresult, PageNumber);
                }

                return View(Membersresult);
            }
        }

        #endregion Members

        #region Member Details

        public ActionResult MemberDetails(int id, string SortOrder, string SortBy, int PageNumber = 1)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                ViewBag.UserID = id;
                var DefaultImg = context.SystemConfigurations.SingleOrDefault(m => m.Key == "DefaultProfileImage").Value;

                //Member Details
                var details = (from User in context.Users
                               where User.UserID == id
                               join Details in context.UserProfile on User.UserID equals Details.UserID
                               join Country in context.Countries on Details.Country equals Country.Name
                               select new MembersModel
                               {
                                   FirstName = User.FirstName,
                                   LastName = User.LastName,
                                   ProfileImage = Details.ProfilePicture == null ? DefaultImg.Remove(0, 2) : Details.ProfilePicture.Remove(0, 2),
                                   Email = User.EmailID,
                                   DOB = Details.DOB,
                                   Phone = Details.PhoneNumber,
                                   University = Details.University,
                                   Address1 = Details.AddressLine1,
                                   Address2 = Details.AddressLine2,
                                   City = Details.City,
                                   State = Details.State,
                                   Country = Country.Name,
                                   Zipcode = Details.ZipCode
                               }).SingleOrDefault();

                ViewBag.Details = details;

                //Member Notes List
                var notes = (from Note in context.SellerNotes
                             where Note.SellerID == id
                             join Status in context.ReferenceData on Note.Status equals Status.ReferenceDataID
                             join Category in context.NoteCategories on Note.Category equals Category.NoteCategoriesID
                             let downloadedNotes = (context.Downloads.Where(m => m.Downloader == id && m.NoteID == Note.SellerNotesID && m.IsAttachmentDownloaded == true).Count())
                             let earning = (context.Downloads.Where(m => m.Seller == id).Sum(x => x.PurchasedPrice))
                             select new MemberNoteModel
                             {
                                 NoteId = Note.SellerNotesID,
                                 Title = Note.Title,
                                 Category = Category.Name,
                                 Status = Status.Value,
                                 DownloadedNote = downloadedNotes,
                                 Earning = earning,
                                 DateAdded = Note.CreatedDate,
                                 PublishedDate = Note.PublishedDate
                             }).OrderByDescending(x => x.DateAdded).ToList();

                ViewBag.SortOrder = SortOrder;
                ViewBag.SortBy = SortBy;
                var MemberDetailsNoteresult = notes;

                //Sorting
                MemberDetailsNoteresult = ApplySorting(SortOrder, SortBy, MemberDetailsNoteresult);

                //Pagination
                MemberDetailsNoteresult = ApplyPagination(MemberDetailsNoteresult, PageNumber);

                return View(MemberDetailsNoteresult);
            }
        }

        #endregion Members Details

        #region Spam Reports

        public ActionResult SpamReports(string txtSearch, string SortOrder, string SortBy, int PageNumber = 1)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var data = (from Spam in context.SellerNotesReportedIssues
                            join Note in context.SellerNotes on Spam.NoteID equals Note.SellerNotesID
                            join User in context.Users on Spam.ReportedByID equals User.UserID
                            join Category in context.NoteCategories on Note.Category equals Category.NoteCategoriesID
                            select new SpamNotesModel
                            {
                                ID = Spam.SellerNotesReportedIssuesID,
                                NoteId = Spam.NoteID,
                                Title = Note.Title,
                                ReportedBy = User.FirstName + " " + User.LastName,
                                Remarks = Spam.Remarks,
                                Category = Category.Name,
                                DateAdded = Spam.CreatedDate
                            }).OrderByDescending(x => x.DateAdded).ToList();

                ViewBag.SortOrder = SortOrder;
                ViewBag.SortBy = SortBy;
                var SpamReportsresult = data;

                if (txtSearch != null)
                {
                    SpamReportsresult = SpamReportsresult.Where(x => x.Title.ToLower().Contains(txtSearch.ToLower())).ToList();

                    //Sorting
                    SpamReportsresult = ApplySorting(SortOrder, SortBy, SpamReportsresult);

                    //Pagination
                    SpamReportsresult = ApplyPagination(SpamReportsresult, PageNumber);
                }
                else
                {
                    //Sorting
                    SpamReportsresult = ApplySorting(SortOrder, SortBy, SpamReportsresult);

                    //Pagination
                    SpamReportsresult = ApplyPagination(SpamReportsresult, PageNumber);
                }

                return View(SpamReportsresult);
            }
        }

        #region Delete Spam Report

        [HttpPost]
        public void DeleteSpamReport(int Id)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var report = context.SellerNotesReportedIssues.Single(m => m.SellerNotesReportedIssuesID == Id);
                context.SellerNotesReportedIssues.Remove(report);
                context.SaveChanges();
            }
        }

        #endregion Delete Spam Report

        #endregion Spam Reports

        #region My Profile

        public ActionResult MyProfile()
        {
            ViewBag.Show = false;

            using (var context = new NotesMarketPlaceEntities())
            {
                var country = context.Countries.ToList();

                var CountryCodeList = country;
                ViewBag.CountryCodeList = new SelectList(CountryCodeList, "CountriesID", "CountryCode");

                var currentAdmin = (from Admin in context.Users
                                    where Admin.EmailID == User.Identity.Name
                                    join Details in context.UserProfile on Admin.UserID equals Details.UserID
                                    select new MyProfile
                                    {
                                        ID = Admin.UserID,
                                        FirstName = Admin.FirstName,
                                        LastName = Admin.LastName,
                                        Email = Admin.EmailID,
                                        SecondaryEmail = Details.SecondaryEmailAddress,
                                        Phonecode = Details.PhoneNumberCountryCode,
                                        Phone = Details.PhoneNumber,
                                        ProfileImage = Details.ProfilePicture
                                    }).Single();

                //.Image = currentAdmin.ProfileImage;

                if (currentAdmin.ProfileImage == null)
                {
                    currentAdmin.ProfileImage = "~/Content/images/upload-file.png";
                    ViewBag.ProfilePicture = "~/Content/images/upload-file.png";
                    ViewBag.ProfilePicturePreview = "#";
                    ViewBag.HideClass = "";
                    ViewBag.NonHideClass = "hidden";
                    ViewBag.ProfilePictureName = "";
                }
                else
                {
                    ViewBag.ProfilePicture = "~/Content/images/upload-file.png";
                    ViewBag.ProfilePicturePreview = currentAdmin.ProfileImage;
                    ViewBag.ProfilePictureName = Path.GetFileNameWithoutExtension(currentAdmin.ProfileImage);
                    ViewBag.HideClass = "hidden";
                    ViewBag.NonHideClass = "";
                }

                return View(currentAdmin);
            }
        }

        [HttpPost]
        public ActionResult MyProfile(MyProfile profile)
        {
            ViewBag.Show = false;

            using (var context = new NotesMarketPlaceEntities())
            {
                if (!ModelState.IsValid)
                {
                    return View(profile);
                }

                var user = context.Users.Single(x => x.EmailID == User.Identity.Name);

                var details = context.UserProfile.Single(x => x.UserID == user.UserID);

                user.FirstName = profile.FirstName;
                user.LastName = profile.LastName;
                details.SecondaryEmailAddress = profile.SecondaryEmail;
                details.PhoneNumber = profile.Phone;
                details.PhoneNumberCountryCode = profile.Phonecode;

                if (profile.UserProfilePicturePath == null)
                {
                    details.ProfilePicture = profile.ProfileImage;
                }
                else
                {
                    string FileNameDelete = System.IO.Path.GetFileName(details.ProfilePicture);
                    string PathPreview = Request.MapPath("~/Members/" + user.UserID + "/" + FileNameDelete);
                    FileInfo file = new FileInfo(PathPreview);
                    if (file.Exists)
                    {
                        file.Delete();
                    }
                    //UserProfilePicturePath
                    string userProfilePicturePathFileName = Path.GetFileNameWithoutExtension(profile.UserProfilePicturePath.FileName);
                    string userProfilePicturePathExtension = Path.GetExtension(profile.UserProfilePicturePath.FileName);
                    userProfilePicturePathFileName = userProfilePicturePathFileName + DateTime.Now.ToString("yymmssff") + userProfilePicturePathExtension;
                    details.ProfilePicture = "~/Members/" + user.UserID + "/" + userProfilePicturePathFileName;
                    userProfilePicturePathFileName = Path.Combine(Server.MapPath("~/Members/" + user.UserID + "/"), userProfilePicturePathFileName);
                    CreateDirectory(user.UserID);
                    profile.UserProfilePicturePath.SaveAs(userProfilePicturePathFileName);
                    //details.ProfilePicture = "../Members/" + user.UserID + "/" + profile.UserProfilePicturePath.FileName;
                }

                user.ModifiedBy = user.UserID;
                user.ModifiedDate = DateTime.Now;
                details.ModifiedDate = DateTime.Now;

                context.SaveChanges();

                var country = context.Countries.ToList();

                var CountryCodeList = country;
                ViewBag.CountryCodeList = new SelectList(CountryCodeList, "CountriesID", "CountryCode");

                ViewBag.Show = true;
                ViewBag.message = "Your profile has been updated successfully.";

                return RedirectToAction("MyProfile");
            }
        }

        #endregion My Profile

        #region Manage Category

        public ActionResult ManageCategory(string txtSearch, string SortOrder, string SortBy, int PageNumber = 1)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var data = (from Category in context.NoteCategories
                            join User in context.Users on Category.CreatedBy equals User.UserID
                            select new ManageCategoryModel
                            {
                                Id = Category.NoteCategoriesID,
                                Name = Category.Name,
                                Description = Category.Description,
                                CreatedDate = Category.CreatedDate,
                                CreatedBy = User.UserID,
                                IsActive = Category.IsActive == true ? "Yes" : "No",
                            }).OrderByDescending(x => x.CreatedDate).ToList();

                ViewBag.SortOrder = SortOrder;
                ViewBag.SortBy = SortBy;
                var ManageCategoryresult = data;

                if (txtSearch != null)
                {
                    ManageCategoryresult = ManageCategoryresult.Where(x => x.Name.ToLower().Contains(txtSearch.ToLower())).ToList();

                    //Sorting
                    ManageCategoryresult = ApplySorting(SortOrder, SortBy, ManageCategoryresult);

                    //Pagination
                    ManageCategoryresult = ApplyPagination(ManageCategoryresult, PageNumber);
                }
                else
                {
                    //Sorting
                    ManageCategoryresult = ApplySorting(SortOrder, SortBy, ManageCategoryresult);

                    //Pagination
                    ManageCategoryresult = ApplyPagination(ManageCategoryresult, PageNumber);
                }

                return View(ManageCategoryresult);
            }
        }

        #region Add Category

        public ActionResult AddCategory(int? edit)
        {
            ViewBag.Edit = false;

            if (edit != null)
            {
                using (var context = new NotesMarketPlaceEntities())
                {
                    var data = context.NoteCategories.Where(m => m.NoteCategoriesID == edit)
                    .Select(x => new AddCategoryModel
                    {
                        Id = x.NoteCategoriesID,
                        Name = x.Name,
                        Description = x.Description
                    }).Single();

                    ViewBag.Edit = true;

                    return View(data);
                }
            }
            return View();
        }

        [HttpPost]
        public ActionResult AddCategory(AddCategoryModel model, int? id)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var context = new NotesMarketPlaceEntities())
            {
                var currentAdmin = context.Users.Single(m => m.EmailID == User.Identity.Name).UserID;

                if (id.Equals(null))
                {
                    //Add New Category
                    var create = context.NoteCategories;
                    create.Add(new NoteCategories
                    {
                        Name = model.Name,
                        Description = model.Description,
                        CreatedBy = currentAdmin,
                        CreatedDate = DateTime.Now,
                        ModifiedBy = currentAdmin,
                        ModifiedDate = DateTime.Now,
                        IsActive = true
                    });

                    context.SaveChanges();
                }
                //Update Existing Category
                else
                {
                    var update = context.NoteCategories.Single(m => m.NoteCategoriesID == id);

                    update.Name = model.Name;
                    update.Description = model.Description;

                    update.ModifiedBy = currentAdmin;
                    update.ModifiedDate = DateTime.Now;
                    update.IsActive = true;

                    context.SaveChanges();
                }

            }
            return RedirectToAction("ManageCategory");
        }

        #endregion Add Category

        #endregion Manage Category

        #region Manage Type

        public ActionResult ManageType(string txtSearch, string SortOrder, string SortBy, int PageNumber = 1)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var data = (from Type in context.NoteTypes
                            join User in context.Users on Type.CreatedBy equals User.UserID
                            select new ManageTypeModel
                            {
                                Id = Type.NoteTypesID,
                                Name = Type.Name,
                                Description = Type.Description,
                                CreatedDate = Type.CreatedDate,
                                CreatedBy = User.UserID,
                                IsActive = Type.IsActive == true ? "Yes" : "No"
                            }).OrderByDescending(x => x.CreatedDate).ToList();

                ViewBag.SortOrder = SortOrder;
                ViewBag.SortBy = SortBy;
                var ManageTyperesult = data;

                if (txtSearch != null)
                {
                    ManageTyperesult = ManageTyperesult.Where(x => x.Name.ToLower().Contains(txtSearch.ToLower())).ToList();

                    //Sorting
                    ManageTyperesult = ApplySorting(SortOrder, SortBy, ManageTyperesult);

                    //Pagination
                    ManageTyperesult = ApplyPagination(ManageTyperesult, PageNumber);
                }
                else
                {
                    //Sorting
                    ManageTyperesult = ApplySorting(SortOrder, SortBy, ManageTyperesult);

                    //Pagination
                    ManageTyperesult = ApplyPagination(ManageTyperesult, PageNumber);
                }

                return View(ManageTyperesult);
            }
        }

        #region Add Type

        public ActionResult AddType(int? edit)
        {
            ViewBag.Edit = false;

            if (edit != null)
            {
                using (var context = new NotesMarketPlaceEntities())
                {
                    var data = context.NoteTypes.Where(m => m.NoteTypesID == edit)
                        .Select(x => new AddTypeModel
                        {
                            Id = x.NoteTypesID,
                            Name = x.Name,
                            Description = x.Description
                        }).Single();

                    ViewBag.Edit = true;

                    return View(data);
                }
            }

            return View();
        }

        [HttpPost]
        public ActionResult AddType(AddTypeModel model, int? id)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var context = new NotesMarketPlaceEntities())
            {
                var currentAdmin = context.Users.Single(m => m.EmailID == User.Identity.Name).UserID;

                if (id.Equals(null))
                {
                    //Add New Category
                    var create = context.NoteTypes;
                    create.Add(new NoteTypes
                    {
                        Name = model.Name,
                        Description = model.Description,
                        CreatedBy = currentAdmin,
                        CreatedDate = DateTime.Now,
                        ModifiedBy = currentAdmin,
                        ModifiedDate = DateTime.Now,
                        IsActive = true
                    });

                    context.SaveChanges();
                }
                //Update Existing Category
                else
                {
                    var update = context.NoteTypes.Single(m => m.NoteTypesID == id);

                    update.Name = model.Name;
                    update.Description = model.Description;

                    update.ModifiedBy = currentAdmin;
                    update.ModifiedDate = DateTime.Now;
                    update.IsActive = true;

                    context.SaveChanges();
                }

            }
            return RedirectToAction("ManageType");
        }

        #endregion Add Type

        #endregion Manage Type

        #region Manage Countries

        public ActionResult ManageCountries(string txtSearch, string SortOrder, string SortBy, int PageNumber = 1)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var data = (from Country in context.Countries
                            join User in context.Users on Country.CreatedBy equals User.UserID
                            select new ManageCountryModel
                            {
                                Id = Country.CountriesID,
                                CountryName = Country.Name,
                                CountryCode = Country.CountryCode,
                                CreatedDate = Country.CreatedDate,
                                CreatedBy = User.UserID,
                                IsActive = Country.IsActive == true ? "Yes" : "No"
                            }).OrderByDescending(x => x.CreatedDate).ToList();

                ViewBag.SortOrder = SortOrder;
                ViewBag.SortBy = SortBy;
                var ManageCountriesresult = data;

                if (txtSearch != null)
                {
                    ManageCountriesresult = ManageCountriesresult.Where(x => x.CountryName.ToLower().Contains(txtSearch.ToLower())).ToList();

                    //Sorting
                    ManageCountriesresult = ApplySorting(SortOrder, SortBy, ManageCountriesresult);

                    //Pagination
                    ManageCountriesresult = ApplyPagination(ManageCountriesresult, PageNumber);
                }
                else
                {
                    //Sorting
                    ManageCountriesresult = ApplySorting(SortOrder, SortBy, ManageCountriesresult);

                    //Pagination
                    ManageCountriesresult = ApplyPagination(ManageCountriesresult, PageNumber);
                }

                return View(ManageCountriesresult);
            }
        }

        #region Add Country

        public ActionResult AddCountry(int? edit)
        {
            ViewBag.Edit = false;

            if (edit != null)
            {
                using (var context = new NotesMarketPlaceEntities())
                {
                    var data = context.Countries.Where(m => m.CountriesID == edit)
                        .Select(x => new AddCountryModel
                        {
                            Id = x.CountriesID,
                            CountryName = x.Name,
                            CountryCode = x.CountryCode
                        }).Single();

                    ViewBag.Edit = true;

                    return View(data);
                }
            }

            return View();
        }

        [HttpPost]
        public ActionResult AddCountry(AddCountryModel model, int? id)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var context = new NotesMarketPlaceEntities())
            {
                var currentAdmin = context.Users.Single(m => m.EmailID == User.Identity.Name).UserID;

                if (id.Equals(null))
                {
                    //Add New Category
                    var create = context.Countries;
                    create.Add(new Countries
                    {
                        Name = model.CountryName,
                        CountryCode = model.CountryCode,
                        CreatedBy = currentAdmin,
                        CreatedDate = DateTime.Now,
                        ModifiedBy = currentAdmin,
                        ModifiedDate = DateTime.Now,
                        IsActive = true
                    });

                    context.SaveChanges();
                }
                //Update Existing Category
                else
                {
                    var update = context.Countries.Single(m => m.CountriesID == id);

                    update.Name = model.CountryName;
                    update.CountryCode = model.CountryCode;

                    update.ModifiedBy = currentAdmin;
                    update.ModifiedDate = DateTime.Now;
                    update.IsActive = true;

                    context.SaveChanges();
                }

            }
            return RedirectToAction("ManageCountries");
        }

        #endregion Add Country

        #endregion Manage Countries

        #region Delete Category, Type, Country

        [HttpPost]
        public ActionResult DeleteSystemConfigItem(int id, string item)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var currentAdmin = context.Users.Single(m => m.EmailID == User.Identity.Name).UserID;
                var return1 = "";
                switch (item)
                {
                    case "Category":
                        var category = context.NoteCategories.Single(m => m.NoteCategoriesID == id);
                        category.IsActive = false;
                        category.ModifiedBy = currentAdmin;
                        category.ModifiedDate = DateTime.Now;
                        context.SaveChanges();
                        return1 = "ManageCategory";

                        break;
                    case "Type":
                        var type = context.NoteTypes.Single(m => m.NoteTypesID == id);
                        type.IsActive = false;
                        type.ModifiedBy = currentAdmin;
                        type.ModifiedDate = DateTime.Now;
                        context.SaveChanges();
                        return1 = "ManageType";

                        break;
                    case "Country":
                        var country = context.Countries.Single(m => m.CountriesID == id);
                        country.IsActive = false;
                        country.ModifiedBy = currentAdmin;
                        country.ModifiedDate = DateTime.Now;
                        context.SaveChanges();
                        return1 = "ManageCountries";

                        break;
                }
                return RedirectToAction(return1, "Admin");
            }
        }

        #endregion Delete Category, Type, Country

        #region Apply Sorting

        public List<DashboardModel> ApplySorting(string SortOrder, string SortBy, List<DashboardModel> result)
        {
            switch (SortBy)
            {
                case "Category":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Category).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Category).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Category).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Title":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Title).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Title).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Title).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "AttachmentSize":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.AttachmentSize).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.AttachmentSize).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.AttachmentSize).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "SellType":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.IsPaid).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.IsPaid).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.IsPaid).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Price":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Price).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Price).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Price).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Publisher":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Publisher).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Publisher).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Publisher).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "PublishedDate":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.PublishDate).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.PublishDate).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.PublishDate).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "NumberofDownloads":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.TotalDownloads).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.TotalDownloads).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.TotalDownloads).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    result = result.OrderByDescending(x => x.PublishDate).ToList();
                    break;
            }
            return result;
        }

        public List<MembersModel> ApplySorting(string SortOrder, string SortBy, List<MembersModel> result)
        {
            switch (SortBy)
            {
                case "FirstName":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.FirstName).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.FirstName).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.FirstName).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "LastName":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.LastName).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.LastName).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.LastName).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Email":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Email).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Email).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Email).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "JoiningDate":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.JoinDate).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.JoinDate).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.JoinDate).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "UnderReviewNotes":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.UnderReviewNotes).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.UnderReviewNotes).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.UnderReviewNotes).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "DownloadedNotes":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.DownloadedNotes).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.DownloadedNotes).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.DownloadedNotes).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "TotalExpenses":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.TotalExpense).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.TotalExpense).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.TotalExpense).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "TotalEarnings":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.TotalEarning).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.TotalEarning).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.TotalEarning).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "PublishedNotes":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.PublishedNotes).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.PublishedNotes).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.PublishedNotes).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    result = result.OrderByDescending(x => x.JoinDate).ToList();
                    break;
            }
            return result;
        }

        public List<MemberNoteModel> ApplySorting(string SortOrder, string SortBy, List<MemberNoteModel> result)
        {
            switch (SortBy)
            {
                case "Title":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Title).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Title).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Title).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Category":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Category).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Category).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Category).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Status":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Status).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Status).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Status).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "DownloadedNotes":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.DownloadedNote).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.DownloadedNote).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.DownloadedNote).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "TotalEarnings":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Earning).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Earning).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Earning).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "DateAdded":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.DateAdded).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.DateAdded).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.DateAdded).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "PublishedDate":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.PublishedDate).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.PublishedDate).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.PublishedDate).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    result = result.OrderByDescending(x => x.DateAdded).ToList();
                    break;
            }
            return result;
        }

        public List<RejectedNotesModel> ApplySorting(string SortOrder, string SortBy, List<RejectedNotesModel> result)
        {
            switch (SortBy)
            {
                case "Title":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Title).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Title).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Title).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Category":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Category).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Category).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Category).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Seller":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.SellerName).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.SellerName).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.SellerName).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "DateEdited":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.PublishedDate).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.PublishedDate).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.PublishedDate).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "RejectedBy":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.RejectedBy).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.RejectedBy).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.RejectedBy).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Remarks":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Remarks).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Remarks).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Remarks).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    result = result.OrderByDescending(x => x.PublishedDate).ToList();
                    break;
            }
            return result;
        }

        public List<DownloadedNotesModel> ApplySorting(string SortOrder, string SortBy, List<DownloadedNotesModel> result)
        {
            switch (SortBy)
            {
                case "Title":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Title).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Title).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Title).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Category":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Category).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Category).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Category).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Buyer":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.BuyerName).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.BuyerName).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.BuyerName).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Seller":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.SellerName).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.SellerName).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.SellerName).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "SellType":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.IsPaid).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.IsPaid).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.IsPaid).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Price":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Price).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Price).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Price).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "DownloadedDate":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.DownloadedDate).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.DownloadedDate).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.DownloadedDate).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    result = result.OrderByDescending(x => x.DownloadedDate).ToList();
                    break;
            }
            return result;
        }

        public List<PublishedNoteModel> ApplySorting(string SortOrder, string SortBy, List<PublishedNoteModel> result)
        {
            switch (SortBy)
            {
                case "Title":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Title).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Title).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Title).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Category":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Category).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Category).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Category).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "SellType":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.IsPaid).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.IsPaid).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.IsPaid).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Price":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Price).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Price).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Price).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Seller":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Seller).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Seller).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Seller).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "PublishedDate":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.PublishDate).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.PublishDate).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.PublishDate).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "ApprovedBy":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.ApprovedBy).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.ApprovedBy).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.ApprovedBy).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "NumberofDownloads":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.TotalDownloads).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.TotalDownloads).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.TotalDownloads).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    result = result.OrderByDescending(x => x.PublishDate).ToList();
                    break;
            }
            return result;
        }

        public List<NotesUnderReviewModel> ApplySorting(string SortOrder, string SortBy, List<NotesUnderReviewModel> result)
        {
            switch (SortBy)
            {
                case "Title":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Title).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Title).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Title).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Category":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Category).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Category).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Category).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Seller":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Seller).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Seller).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Seller).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "DateAdded":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.CreatedDate).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.CreatedDate).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.CreatedDate).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Status":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.status).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.status).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.status).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    result = result.OrderByDescending(x => x.CreatedDate).ToList();
                    break;
            }
            return result;
        }

        public List<SpamNotesModel> ApplySorting(string SortOrder, string SortBy, List<SpamNotesModel> result)
        {
            switch (SortBy)
            {
                case "ReportedBy":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.ReportedBy).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.ReportedBy).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.ReportedBy).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Title":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Title).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Title).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Title).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Category":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Category).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Category).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Category).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "DateEdited":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.DateAdded).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.DateAdded).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.DateAdded).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Remark":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Remarks).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Remarks).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Remarks).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    result = result.OrderByDescending(x => x.DateAdded).ToList();
                    break;
            }
            return result;
        }

        public List<ManageCategoryModel> ApplySorting(string SortOrder, string SortBy, List<ManageCategoryModel> result)
        {
            switch (SortBy)
            {
                case "Category":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Name).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Name).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Name).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Description":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Description).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Description).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Description).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "DateAdded":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.CreatedDate).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.CreatedDate).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.CreatedDate).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "AddedBy":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.CreatedBy).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.CreatedBy).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.CreatedBy).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Active":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.IsActive).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.IsActive).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.IsActive).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    result = result.OrderByDescending(x => x.CreatedDate).ToList();
                    break;
            }
            return result;
        }

        public List<ManageTypeModel> ApplySorting(string SortOrder, string SortBy, List<ManageTypeModel> result)
        {
            switch (SortBy)
            {
                case "Type":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Name).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Name).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Name).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Description":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Description).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Description).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Description).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "DateAdded":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.CreatedDate).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.CreatedDate).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.CreatedDate).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "AddedBy":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.CreatedBy).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.CreatedBy).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.CreatedBy).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Active":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.IsActive).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.IsActive).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.IsActive).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    result = result.OrderByDescending(x => x.CreatedDate).ToList();
                    break;
            }
            return result;
        }

        public List<ManageCountryModel> ApplySorting(string SortOrder, string SortBy, List<ManageCountryModel> result)
        {
            switch (SortBy)
            {
                case "CountryName":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.CountryName).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.CountryName).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.CountryName).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "CountryCode":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.CountryCode).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.CountryCode).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.CountryCode).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "DateAdded":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.CreatedDate).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.CreatedDate).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.CreatedDate).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "AddedBy":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.CreatedBy).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.CreatedBy).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.CreatedBy).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Active":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.IsActive).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.IsActive).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.IsActive).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    result = result.OrderByDescending(x => x.CreatedDate).ToList();
                    break;
            }
            return result;
        }

        #endregion Apply Sorting

        #region Apply Pagination

        public List<DashboardModel> ApplyPagination(List<DashboardModel> result, int PageNumber)
        {
            ViewBag.TotalPages = Math.Ceiling(result.Count() / 5.0);
            ViewBag.PageNumber = PageNumber;

            result = result.Skip((PageNumber - 1) * 5).Take(5).ToList();

            return result;
        }

        public List<MembersModel> ApplyPagination(List<MembersModel> result, int PageNumber)
        {
            ViewBag.TotalPages = Math.Ceiling(result.Count() / 5.0);
            ViewBag.PageNumber = PageNumber;

            result = result.Skip((PageNumber - 1) * 5).Take(5).ToList();

            return result;
        }

        public List<MemberNoteModel> ApplyPagination(List<MemberNoteModel> result, int PageNumber)
        {
            ViewBag.TotalPages = Math.Ceiling(result.Count() / 5.0);
            ViewBag.PageNumber = PageNumber;

            result = result.Skip((PageNumber - 1) * 5).Take(5).ToList();

            return result;
        }

        public List<RejectedNotesModel> ApplyPagination(List<RejectedNotesModel> result, int PageNumber)
        {
            ViewBag.TotalPages = Math.Ceiling(result.Count() / 5.0);
            ViewBag.PageNumber = PageNumber;

            result = result.Skip((PageNumber - 1) * 5).Take(5).ToList();

            return result;
        }

        public List<DownloadedNotesModel> ApplyPagination(List<DownloadedNotesModel> result, int PageNumber)
        {
            ViewBag.TotalPages = Math.Ceiling(result.Count() / 5.0);
            ViewBag.PageNumber = PageNumber;

            result = result.Skip((PageNumber - 1) * 5).Take(5).ToList();

            return result;
        }

        public List<PublishedNoteModel> ApplyPagination(List<PublishedNoteModel> result, int PageNumber)
        {
            ViewBag.TotalPages = Math.Ceiling(result.Count() / 5.0);
            ViewBag.PageNumber = PageNumber;

            result = result.Skip((PageNumber - 1) * 5).Take(5).ToList();

            return result;
        }

        public List<NotesUnderReviewModel> ApplyPagination(List<NotesUnderReviewModel> result, int PageNumber)
        {
            ViewBag.TotalPages = Math.Ceiling(result.Count() / 5.0);
            ViewBag.PageNumber = PageNumber;

            result = result.Skip((PageNumber - 1) * 5).Take(5).ToList();

            return result;
        }

        public List<SpamNotesModel> ApplyPagination(List<SpamNotesModel> result, int PageNumber)
        {
            ViewBag.TotalPages = Math.Ceiling(result.Count() / 5.0);
            ViewBag.PageNumber = PageNumber;

            result = result.Skip((PageNumber - 1) * 5).Take(5).ToList();

            return result;
        }

        public List<ManageCategoryModel> ApplyPagination(List<ManageCategoryModel> result, int PageNumber)
        {
            ViewBag.TotalPages = Math.Ceiling(result.Count() / 5.0);
            ViewBag.PageNumber = PageNumber;

            result = result.Skip((PageNumber - 1) * 5).Take(5).ToList();

            return result;
        }

        public List<ManageTypeModel> ApplyPagination(List<ManageTypeModel> result, int PageNumber)
        {
            ViewBag.TotalPages = Math.Ceiling(result.Count() / 5.0);
            ViewBag.PageNumber = PageNumber;

            result = result.Skip((PageNumber - 1) * 5).Take(5).ToList();

            return result;
        }

        public List<ManageCountryModel> ApplyPagination(List<ManageCountryModel> result, int PageNumber)
        {
            ViewBag.TotalPages = Math.Ceiling(result.Count() / 5.0);
            ViewBag.PageNumber = PageNumber;

            result = result.Skip((PageNumber - 1) * 5).Take(5).ToList();

            return result;
        }

        #endregion Apply Pagination

        #region Returns File Size in KB

        public float GetSize(int user, int note, string filename)
        {
            string filePath = Server.MapPath(filename);
            System.IO.FileStream fs = System.IO.File.OpenRead(filePath);
            return (fs.Length / 1000);
        }

        #endregion Returns File Size in KB

        #region Download File

        public FileResult DownloadFile(int noteid)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var file = (from Attachment in context.SellerNotesAttachements
                            where Attachment.NoteID == noteid
                            join Note in context.SellerNotes on Attachment.NoteID equals Note.SellerNotesID
                            select new
                            {
                                Note.SellerID,
                                Attachment.FileName
                            }).SingleOrDefault();

                string filepath = Server.MapPath("../Content/NotesImages/NotesPDF/" + file.FileName);
                byte[] filebyte = GetFile(filepath);

                return File(filebyte, System.Net.Mime.MediaTypeNames.Application.Octet, file.FileName);
            }
        }

        #endregion Download File

        #region Return File

        byte[] GetFile(string s)
        {
            System.IO.FileStream fs = System.IO.File.OpenRead(s);
            byte[] data = new byte[fs.Length];
            int br = fs.Read(data, 0, data.Length);
            if (br != fs.Length)
            {
                throw new System.IO.IOException(s);
            }
            return data;
        }

        #endregion Return File

        #region Create Directory

        public string CreateDirectory(int userid)
        {
            string path = @"E:\GitHub\TatvaSoft\NoteMarketPlaceHTML\MVC\NotesMarketPlace\NotesMarketPlace\Members\" + userid;

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return path;
            }
            else
            {
                return null;
            }
        }

        #endregion Create Directory

        #region LogOut

        public ActionResult LogOut()
        {
            //if (Session["emailID"] != null)
            //{
            //    Session.Abandon();
            //    return RedirectToAction("Login", "Account");
            //}
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Account");
        }

        #endregion LogOut
    }
}