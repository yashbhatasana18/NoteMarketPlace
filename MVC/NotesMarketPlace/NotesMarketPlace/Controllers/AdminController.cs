using System.Linq;
using System.Web.Mvc;
using NotesMarketPlace.DB;
using NotesMarketPlace.DB.DBOperations;
using NotesMarketPlace.Models.Admin;
using System.Web.Security;
using System.Web.Hosting;
using System.Text;
using System.Net.Mail;
using System;
using System.Drawing;
using System.IO;
using System.Web.Routing;
using System.Collections.Generic;

namespace NotesMarketPlace.Controllers
{
    [Authorize(Roles = "Admin, SuperAdmin")]
    public class AdminController : Controller
    {
        AdminRepository adminRepository = null;

        #region Default Constructor
        public AdminController()
        {
            adminRepository = new AdminRepository();

            using (var context = new NotesMarketPlaceEntities())
            {
                // set social URL
                var socialUrl = context.SystemConfigurations.Where(m => m.Key == "Facebook" || m.Key == "Twitter" || m.Key == "Linkedin").ToList();
                ViewBag.URLs = socialUrl;
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
                    // get current user
                    var currentUser = context.Users.FirstOrDefault(m => m.EmailID == User.Identity.Name);

                    //current user profile image
                    var img = (from Details in context.UserProfile
                               join Users in context.Users on Details.UserID equals Users.UserID
                               where Users.EmailID == requestContext.HttpContext.User.Identity.Name
                               select Details.ProfilePicture).FirstOrDefault();

                    string fileName = System.IO.Path.GetFileName(img);

                    string filePath = "Members/" + currentUser.UserID + "/" + fileName;

                    if (img == null)
                    {
                        // set default image
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

        #region Dashboard

        public ActionResult Dashboard(int? month, string txtSearch, string SortOrder, string SortBy, int PageNumber = 1)
        {
            //if (Session["emailID"] == null)
            //{
            //    return RedirectToAction("Login", "Account");
            //}

            using (var context = new NotesMarketPlaceEntities())
            {
                // total notes in review
                var inReviewNote = context.SellerNotes.Where(m => (m.Status == 7 || m.Status == 8) && m.IsActive == true).Count();
                // total notes downloaded (last 7 days)
                var condition = DateTime.Now.Date.AddDays(-7);
                var downloads = context.Downloads.Where(m => m.IsSellerHasAllowedDownload == true && m.IsAttachmentDownloaded == true && m.AttachmentDownloadedDate >= condition).Count();
                // total new Registration (last 7 days)
                var registration = context.Users.Where(m => m.CreatedDate >= condition).Count();

                ViewBag.InReview = inReviewNote;
                ViewBag.Downloads = downloads;
                ViewBag.Registration = registration;

                // last 6 month from today
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

                // published note
                var note = (from Note in context.SellerNotes
                            join Attachment in context.SellerNotesAttachements on Note.SellerNotesID equals Attachment.NoteID
                            join Category in context.NoteCategories on Note.Category equals Category.NoteCategoriesID
                            join User in context.Users on Note.SellerID equals User.UserID
                            where Note.Status == 6
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
                                filename = Attachment.FilePath
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

                // append attachment size
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
                    Membersresult = Membersresult.Where(x => x.FirstName.ToLower().Contains(txtSearch.ToLower())).ToList();
                    Membersresult = Membersresult.Where(x => x.LastName.ToLower().Contains(txtSearch.ToLower())).ToList();
                    Membersresult = Membersresult.Where(x => x.Email.ToLower().Contains(txtSearch.ToLower())).ToList();

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

                return View(model);
            }

        }

        #endregion Members

        #region Member Details

        public ActionResult MemberDetails(int id, string SortOrder, string SortBy, int PageNumber = 1)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                // default img
                var DefaultImg = context.SystemConfigurations.SingleOrDefault(m => m.Key == "DefaultProfileImage").Value;

                // member details
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

                // member notes
                var notes = (from Note in context.SellerNotes
                             where Note.SellerID == id
                             join Status in context.ReferenceData on Note.Status equals Status.ReferenceDataID
                             join Category in context.NoteCategories on Note.Category equals Category.NoteCategoriesID
                             let downloadedNotes = (context.Downloads.Where(m => m.Downloader == id && m.IsAttachmentDownloaded == true).Count())
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
                             }).ToList();

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
                case "Category":
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
                case "Title":
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
                case "Category":
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
                case "Title":
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
                default:
                    result = result.OrderByDescending(x => x.DateAdded).ToList();
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

        #endregion Apply Pagination

        // returns file size in KB
        public float GetSize(int user, int note, string filename)
        {
            string filePath = Server.MapPath(filename);
            System.IO.FileStream fs = System.IO.File.OpenRead(filePath);
            return (fs.Length / 1000);
        }

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
    }
}