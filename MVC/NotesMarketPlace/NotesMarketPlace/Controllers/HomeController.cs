using NotesMarketPlace.DB;
using NotesMarketPlace.DB.DBOperations;
using NotesMarketPlace.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;

namespace NotesMarketPlace.Controllers
{
    [Authorize(Roles = "Admin, Super Admin, Member")]
    public class HomeController : Controller
    {
        AddNotesRepository addNoteRepository = null;
        SellYourNotesRepository sellYourNotesRepository = null;

        #region Default Constructor
        public HomeController()
        {
            addNoteRepository = new AddNotesRepository();
            sellYourNotesRepository = new SellYourNotesRepository();

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

        public ActionResult Index()
        {
            return View();
        }

        #region Sell Your Notes

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult SellYourNotes(string txtSearch1, string SortOrder, string SortBy, string txtSearch2, string SortOrder2, string SortBy2, int PageNumber = 1, int PageNumber2 = 1)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                // get current user
                var currentUser = context.Users.FirstOrDefault(m => m.EmailID == User.Identity.Name);

                // total earning
                var Earning = (from Purchase in context.Downloads
                               where Purchase.Seller == currentUser.UserID && Purchase.IsSellerHasAllowedDownload == true
                               group Purchase by Purchase.Seller into grp
                               select grp.Sum(m => m.PurchasedPrice)).ToList();
                ViewBag.Earning = Earning.Count() == 0 ? 0 : Earning[0];

                // total notes sold
                var SoldNotes = (from Purchase in context.Downloads
                                 where Purchase.Seller == currentUser.UserID && Purchase.IsSellerHasAllowedDownload == true
                                 group Purchase by Purchase.Seller into grp
                                 select grp.Count()).ToList();
                ViewBag.SoldNotes = SoldNotes.Count() == 0 ? 0 : SoldNotes[0];

                // My download notes
                var DownloadedNotes = (from Purchase in context.Downloads
                                       where Purchase.Downloader == currentUser.UserID && Purchase.IsSellerHasAllowedDownload == true
                                       group Purchase by Purchase.Downloader into grp
                                       select grp.Count()).ToList();
                ViewBag.DownloadNotes = DownloadedNotes.Count() == 0 ? 0 : DownloadedNotes[0];

                // My Rejected Notes
                var RejectedNotes = (from Notes in context.SellerNotes
                                     join Status in context.ReferenceData on Notes.Status equals Status.ReferenceDataID
                                     where Status.RefCategory == "Notes Status" && Status.Value == "Rejected" && Notes.SellerID == currentUser.UserID
                                     group Notes by Notes.SellerID into grp
                                     select grp.Count()).ToList();
                ViewBag.RejectedNotes = RejectedNotes.Count() == 0 ? 0 : RejectedNotes[0];

                // Buyer Requests
                var BuyerRequests = (from Purchase in context.Downloads
                                     where Purchase.IsSellerHasAllowedDownload == false && Purchase.Seller == currentUser.UserID
                                     group Purchase by Purchase.Seller into grp
                                     select grp.Count()).ToList();
                ViewBag.BuyerRequest = BuyerRequests.Count() == 0 ? 0 : BuyerRequests[0];

                // in progress notes
                var ProgressNotes = (from Notes in context.SellerNotes
                                     join Category in context.NoteCategories on Notes.Category equals Category.NoteCategoriesID
                                     join Status in context.ReferenceData on Notes.Status equals Status.ReferenceDataID
                                     where Status.RefCategory == "Notes Status" && Notes.SellerID == currentUser.UserID &&
                                     (Status.Value == "Draft" || Status.Value == "Submitted" || Status.Value == "In Review")
                                     select new DashboardModel.UserDashboardInProgressModel
                                     {
                                         Id = Notes.SellerID,
                                         Title = Notes.Title,
                                         Category = Category.Name,
                                         Status = Status.Value,
                                         AddedDate = Notes.PublishedDate
                                     }).OrderByDescending(m => m.AddedDate).ToList();

                // published notes
                var PublishedNotes = (from Notes in context.SellerNotes
                                      join Category in context.NoteCategories on Notes.SellerID equals Category.NoteCategoriesID
                                      join Status in context.ReferenceData on Notes.Status equals Status.ReferenceDataID
                                      where Status.RefCategory == "Notes Status" && Status.Value == "Published" && Notes.SellerID == currentUser.UserID
                                      select new DashboardModel.UserDashboardPublishedNoteModel
                                      {
                                          Id = Notes.SellerID,
                                          Title = Notes.Title,
                                          Category = Category.Name,
                                          Price = Notes.SellingPrice,
                                          SellType = (Notes.SellingPrice == 0 ? "Free" : "Paid"),
                                          AddedDate = Notes.PublishedDate
                                      }).OrderByDescending(m => m.AddedDate).ToList();

                ViewBag.SortOrder = SortOrder;
                ViewBag.SortBy = SortBy;
                ViewBag.SortOrder2 = SortOrder2;
                ViewBag.SortBy2 = SortBy2;

                var ProgressNotesresult = ProgressNotes;
                var PublishedNotesresult = PublishedNotes;

                if (txtSearch1 != null)
                {
                    ProgressNotesresult = ProgressNotesresult.Where(x => x.Title.ToLower().Contains(txtSearch1.ToLower())).ToList();

                    //Sorting
                    ProgressNotesresult = ApplySorting(SortOrder, SortBy, ProgressNotesresult);

                    //Pagination
                    ProgressNotesresult = ApplyPagination(ProgressNotesresult, PageNumber);
                    PublishedNotesresult = ApplyPagination2(PublishedNotesresult, PageNumber2);
                }
                else if (txtSearch2 != null)
                {
                    PublishedNotesresult = PublishedNotesresult.Where(x => x.Title.ToLower().Contains(txtSearch2.ToLower())).ToList();

                    //Sorting
                    PublishedNotesresult = ApplySorting2(SortOrder2, SortBy2, PublishedNotesresult);

                    //Pagination
                    ProgressNotesresult = ApplyPagination(ProgressNotesresult, PageNumber);
                    PublishedNotesresult = ApplyPagination2(PublishedNotesresult, PageNumber2);
                }
                else
                {
                    //Sorting
                    ProgressNotesresult = ApplySorting(SortOrder, SortBy, ProgressNotesresult);
                    PublishedNotesresult = ApplySorting2(SortOrder2, SortBy2, PublishedNotesresult);

                    //Pagination
                    ProgressNotesresult = ApplyPagination(ProgressNotesresult, PageNumber);
                    PublishedNotesresult = ApplyPagination2(PublishedNotesresult, PageNumber2);
                }

                ViewBag.ProgressNotes = ProgressNotesresult;
                ViewBag.PublishedNotes = PublishedNotesresult;

                return View();
            }
        }

        #endregion Sell Your Notes

        #region AddNote

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult AddNotes(AddNotesModel model)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var user = context.Users.FirstOrDefault(x => x.EmailID == User.Identity.Name);
                model.SellerID = user.UserID;
                if (ModelState.IsValid)
                {
                    //NoteDisplayPicturePath
                    string displayPictureFileName = Path.GetFileNameWithoutExtension(model.NoteDisplayPicturePath.FileName);
                    string displayPictureExtension = Path.GetExtension(model.NoteDisplayPicturePath.FileName);
                    displayPictureFileName = displayPictureFileName + DateTime.Now.ToString("yymmssff") + displayPictureExtension;
                    model.DisplayPicture = "~/Content/NotesImages/Images/" + displayPictureFileName;
                    displayPictureFileName = Path.Combine(Server.MapPath("~/Content/NotesImages/Images/"), displayPictureFileName);
                    model.NoteDisplayPicturePath.SaveAs(displayPictureFileName);

                    //NoteUploadFilePath
                    string noteUploadFilePathFileName = Path.GetFileNameWithoutExtension(model.NoteUploadFilePath.FileName);
                    model.FileName = Path.GetFileNameWithoutExtension(model.NoteUploadFilePath.FileName);
                    string noteUploadFilePathExtension = Path.GetExtension(model.NoteUploadFilePath.FileName);
                    noteUploadFilePathFileName = noteUploadFilePathFileName + DateTime.Now.ToString("yymmssff") + noteUploadFilePathExtension;
                    model.FilePath = "~/Content/NotesImages/NotesPDF/" + noteUploadFilePathFileName;
                    noteUploadFilePathFileName = Path.Combine(Server.MapPath("~/Content/NotesImages/NotesPDF/"), noteUploadFilePathFileName);
                    model.NoteUploadFilePath.SaveAs(noteUploadFilePathFileName);

                    //NotePreviewFilePath
                    string notePreviewFilePathFileName = Path.GetFileNameWithoutExtension(model.NotePreviewFilePath.FileName);
                    string notePreviewFilePathExtension = Path.GetExtension(model.NotePreviewFilePath.FileName);
                    notePreviewFilePathFileName = notePreviewFilePathFileName + DateTime.Now.ToString("yymmssff") + notePreviewFilePathExtension;
                    model.NotesPreview = "~/Content/NotesImages/NotesPreview/" + notePreviewFilePathFileName;
                    notePreviewFilePathFileName = Path.Combine(Server.MapPath("~/Content/NotesImages/NotesPreview/"), notePreviewFilePathFileName);
                    model.NotePreviewFilePath.SaveAs(notePreviewFilePathFileName);

                    int id = addNoteRepository.AddNotes(model);

                    if (id > 0)
                    {
                        ModelState.Clear();
                        ViewBag.message = "Your note has been successfully added";
                    }
                }

                var NoteCategoryList = context.NoteCategories.ToList();
                ViewBag.NotesCategory = new SelectList(NoteCategoryList, "NoteCategoriesID", "Name");

                var NotesTypeList = context.NoteTypes.ToList();
                ViewBag.NotesType = new SelectList(NotesTypeList, "NoteTypesID", "Name");

                var CountryList = context.Countries.ToList();
                ViewBag.NotesCountry = new SelectList(CountryList, "CountriesID", "Name");

                return View("AddNotes");
            }
        }

        #endregion  AddNote

        #region Search Notes

        [AllowAnonymous]
        public ActionResult SearchNotes(int? Type, int? Category, string University, string Course, int? Country, int? Rating, string search)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                // get all types
                var type = context.NoteTypes.Where(m => m.IsActive == true).ToList();
                // get all category
                var category = context.NoteCategories.Where(m => m.IsActive == true).ToList();
                // get distinct university
                var university = context.SellerNotes.Where(m => m.UniversityName != null).Select(x => x.UniversityName).Distinct().ToList();
                // get distinct courses
                var course = context.SellerNotes.Where(m => m.Course != null).Select(x => x.Course).Distinct().ToList();
                // get all countries
                var country = context.SellerNotes.Where(m => m.Countries != null).Select(x => x.Countries).Distinct().ToList();

                // get all book details
                var notes = (from Notes in context.SellerNotes
                             join Status in context.ReferenceData on Notes.Status equals Status.ReferenceDataID
                             where Status.Value == "Published" && Notes.IsActive == true
                             let avgRatings = (from Review in context.SellerNotesReviews
                                               where Review.NoteID == Notes.SellerID
                                               group Review by Review.NoteID into grp
                                               select new SellerNotesReviewsModel
                                               {
                                                   Ratings = Math.Round(grp.Average(m => m.Ratings)),
                                                   Total = grp.Count()
                                               })
                             let spamNote = (from Spam in context.SellerNotesReportedIssues
                                             where Spam.NoteID == Notes.SellerID
                                             group Spam by Spam.ReportedByID into grp
                                             select new SellerNotesReportedIssuesModel
                                             {
                                                 Total = grp.Count()
                                             })
                             select new SearchNotesModel
                             {
                                 SellerID = Notes.SellerID,
                                 SellerNotesID = Notes.SellerNotesID,
                                 Title = Notes.Title,
                                 Status = Notes.Status,
                                 ActionedBy = Notes.ActionedBy,
                                 AdminRemarks = Notes.AdminRemarks,
                                 PublishedDate = Notes.PublishedDate,
                                 DisplayPicture = Notes.DisplayPicture,
                                 NoteType = Notes.NoteType,
                                 NumberOfPages = Notes.NumberOfPages,
                                 Description = Notes.Description,
                                 UniversityName = Notes.UniversityName,
                                 Country = Notes.Country,
                                 Course = Notes.Course,
                                 CourseCode = Notes.CourseCode,
                                 Professor = Notes.Professor,
                                 IsPaid = Notes.IsPaid,
                                 SellingPrice = Notes.SellingPrice,
                                 NotesPreview = Notes.NotesPreview,
                                 Reviews = avgRatings.Select(a => a.Ratings).FirstOrDefault(),
                                 TotalReviews = avgRatings.Select(a => a.Total).FirstOrDefault(),
                                 TotalSpams = spamNote.Select(a => a.Total).FirstOrDefault()
                             }).ToList();

                ViewBag.TypeList = type;
                ViewBag.CategoryList = category;
                ViewBag.University = university;
                ViewBag.Course = course;
                ViewBag.Country = country;

                var filternotes = notes;

                // if filter value is available
                if (!Type.Equals(null))
                {
                    filternotes = filternotes.Where(m => m.NoteType == Type).ToList();
                }
                if (!Category.Equals(null))
                {
                    filternotes = filternotes.Where(m => m.Category == Category).ToList();
                }
                if (University != null)
                {
                    filternotes = filternotes.Where(m => m.UniversityName == University).ToList();
                }
                if (Course != null)
                {
                    filternotes = filternotes.Where(m => m.Course == Course).ToList();
                }
                if (!Country.Equals(null))
                {
                    filternotes = filternotes.Where(m => m.Country == Country).ToList();
                }
                if (!Rating.Equals(null))
                {
                    filternotes = filternotes.Where(m => m.Reviews >= Rating).ToList();
                }
                if (search != null)
                {
                    filternotes = filternotes.Where(m => m.Title.ToLower().Contains(search.ToLower())).ToList();
                }
                return View(filternotes);
            }
        }

        #endregion Search Notes

        #region Buyer Request

        public ActionResult BuyerRequest()
        {
            // current login user email
            string userEmail = User.Identity.Name;

            using (var context = new NotesMarketPlaceEntities())
            {
                var result = (from Purchase in context.Downloads
                              join Note in context.SellerNotes on Purchase.NoteID equals Note.SellerNotesID
                              join Downloader in context.Users on Purchase.Downloader equals Downloader.UserID
                              join Seller in context.Users on Purchase.Seller equals Seller.UserID
                              join Category in context.NoteCategories on Note.Category equals Category.NoteCategoriesID
                              join UserProfile in context.UserProfile on Note.SellerID equals UserProfile.UserID
                              where Purchase.IsSellerHasAllowedDownload == false && Seller.EmailID == userEmail
                              select new BuyerRequestModel
                              {
                                  DownloadsID = Purchase.DownloadsID,
                                  NoteID = Purchase.NoteID,
                                  NoteTitle = Note.Title,
                                  NoteCategory = Category.Name,
                                  Seller = Seller.UserID,
                                  Downloader = Downloader.EmailID,
                                  Phone = UserProfile.PhoneNumber,
                                  IsPaid = Purchase.PurchasedPrice == 0 ? "Free" : "Paid",
                                  PurchasedPrice = Purchase.PurchasedPrice,
                                  AttachmentDownloadedDate = Purchase.AttachmentDownloadedDate
                              }).OrderByDescending(m => m.AttachmentDownloadedDate).ToList();

                return View(result);

            }
        }

        #endregion Buyer Request

        #region AllowDownload

        // allow buyer to download note
        [HttpPost]
        [Route("User/AllowDownload")]
        public HttpStatusCodeResult AllowDownload(int id)
        {
            if (id.Equals(null))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var context = new NotesMarketPlaceEntities())
            {
                // search purchase details by id
                var result = context.Downloads.FirstOrDefault(m => m.NoteID == id);
                var seller = context.Users.FirstOrDefault(m => m.UserID == result.Seller);
                var downloader = context.Users.FirstOrDefault(m => m.UserID == result.Downloader);

                // if result not available
                if (result != null)
                {
                    // set allowDownload true
                    result.IsSellerHasAllowedDownload = true;
                    context.SaveChanges();

                    // send mail to buyer
                    string subject = seller.FirstName + " Allows you to download a note";
                    string body = "Hello " + downloader.FirstName + "\\n"
                        + "We would like to inform you that, " + seller.FirstName + " Allows you to download a note. Please login and see My Download tabs to download particular note.";
                    body += "\\nRegards,\\nNotes MarketPlace";

                    bool isSend = SendEmailUser.EmailSend(downloader.EmailID, subject, body, false);

                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                else
                {
                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                }
            }
        }

        #endregion AllowDownload

        #region ContactUs

        [AllowAnonymous]
        public ActionResult ContactUs()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult ContactUs(ContactUsModel model)
        {
            string Body = "Hello, <br/><br/>" + model.Comments + "<br/><br/>" + "Regards,<br/>" + model.FullName;

            BuildEmailTemplate(model.EmailID, model.Subject, Body);

            return View();
        }

        public static void BuildEmailTemplate(string sendTo, string subjectText, string bodyText)
        {
            string from, to, bcc, cc, subject, body;
            from = sendTo.Trim();
            to = "ynpatel2000@gmail.com";
            bcc = "";
            cc = "";
            subject = subjectText + " - Query";
            StringBuilder sb = new StringBuilder();
            sb.Append(bodyText);
            body = sb.ToString();
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(from);
            mail.To.Add(new MailAddress(to));
            if (!string.IsNullOrEmpty(bcc))
            {
                mail.Bcc.Add(new MailAddress(bcc));
            }
            if (!string.IsNullOrEmpty(cc))
            {
                mail.CC.Add(new MailAddress(cc));
            }
            mail.Subject = subject;
            mail.Body = body;
            mail.IsBodyHtml = true;
            SendEmail(mail);
        }

        public static void SendEmail(MailMessage mail)
        {
            SmtpClient client = new SmtpClient();
            client.Host = "smtp.gmail.com";
            client.Port = 587;
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Credentials = new System.Net.NetworkCredential("ynpatel2000@gmail.com", "Yash@1852");
            try
            {
                client.Send(mail);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion ContactUs

        [AllowAnonymous]
        public ActionResult Faq()
        {
            return View();
        }

        #region Apply Sorting

        public List<DashboardModel.UserDashboardInProgressModel> ApplySorting(string SortOrder, string SortBy, List<DashboardModel.UserDashboardInProgressModel> result)
        {
            switch (SortBy)
            {
                case "Added Date":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.AddedDate).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.AddedDate).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.AddedDate).ToList();
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
                    result = result.OrderBy(x => x.AddedDate).ToList();
                    break;
            }
            return result;
        }
        public List<DashboardModel.UserDashboardPublishedNoteModel> ApplySorting2(string SortOrder2, string SortBy2, List<DashboardModel.UserDashboardPublishedNoteModel> result)
        {
            switch (SortBy2)
            {
                case "Added Date":
                    {
                        switch (SortOrder2)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.AddedDate).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.AddedDate).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.AddedDate).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Title":
                    {
                        switch (SortOrder2)
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
                    result = result.OrderBy(x => x.AddedDate).ToList();
                    break;
            }
            return result;
        }

        #endregion Apply Sorting

        #region Apply Pagination

        public List<DashboardModel.UserDashboardInProgressModel> ApplyPagination(List<DashboardModel.UserDashboardInProgressModel> result, int PageNumber)
        {
            ViewBag.TotalPages = Math.Ceiling(result.Count() / 5.0);
            ViewBag.PageNumber = PageNumber;

            result = result.Skip((PageNumber - 1) * 5).Take(5).ToList();

            return result;
        }
        public List<DashboardModel.UserDashboardPublishedNoteModel> ApplyPagination2(List<DashboardModel.UserDashboardPublishedNoteModel> result, int PageNumber2)
        {
            ViewBag.TotalPages2 = Math.Ceiling(result.Count() / 5.0);
            ViewBag.PageNumber2 = PageNumber2;

            result = result.Skip((PageNumber2 - 1) * 5).Take(5).ToList();

            return result;
        }

        #endregion Apply Pagination
    }
}