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

        public ActionResult Dashboard(int? month)
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

                // append attachment size
                foreach (var data in note)
                {
                    data.AttachmentSize = GetSize(data.userid, data.Id, data.filename);
                }

                if (month == null)
                {
                    var filternote = note.Where(m => m.publishMonth == DateTime.Now.Month).ToList();
                    return View(filternote);
                }
                else
                {
                    var filternote = note.Where(m => m.publishMonth == month).ToList();
                    return View(filternote);
                }

            }

        }

        #endregion Dashboard

        // returns file size in KB
        public float GetSize(int user, int note, string filename)
        {
            string filePath = Server.MapPath(filename);
            System.IO.FileStream fs = System.IO.File.OpenRead(filePath);
            return (fs.Length / 1000);
        }

        public ActionResult LogOut()
        {
            if (Session["emailID"] != null)
            {
                Session.Abandon();
                return RedirectToAction("Login", "Account");
            }
            //FormsAuthentication.SignOut();
            return View();
        }
    }
}