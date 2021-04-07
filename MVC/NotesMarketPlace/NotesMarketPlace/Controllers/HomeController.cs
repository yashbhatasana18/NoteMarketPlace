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
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace NotesMarketPlace.Controllers
{
    [Authorize(Roles = "Admin, Super Admin, Member")]
    public class HomeController : Controller
    {
        AddNotesRepository addNoteRepository = null;
        SellYourNotesRepository sellYourNotesRepository = null;

        readonly NotesMarketPlaceEntities db;

        [AllowAnonymous]
        public ActionResult Home()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult Faq()
        {
            return View();
        }

        #region Default Constructor
        public HomeController()
        {
            addNoteRepository = new AddNotesRepository();
            sellYourNotesRepository = new SellYourNotesRepository();
            db = new NotesMarketPlaceEntities();

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
                                         Id = Notes.SellerNotesID,
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
                                          Id = Notes.SellerNotesID,
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
        public ActionResult AddNotes(AddNotesModel model, string Command)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var user = context.Users.FirstOrDefault(x => x.EmailID == User.Identity.Name);
                model.SellerID = user.UserID;

                if (user != null && ModelState.IsValid)
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

                    if (model.IsPaid)
                    {
                        if (model.SellingPrice == null || model.SellingPrice < 1)
                        {
                            ModelState.AddModelError("SellingPrice", "Enter valid Selling price");

                            var NoteCategoryList1 = context.NoteCategories.ToList();
                            ViewBag.NotesCategory = new SelectList(NoteCategoryList1, "NoteCategoriesID", "Name");

                            var NotesTypeList1 = context.NoteTypes.ToList();
                            ViewBag.NotesType = new SelectList(NotesTypeList1, "NoteTypesID", "Name");

                            var CountryList1 = context.Countries.ToList();
                            ViewBag.NotesCountry = new SelectList(CountryList1, "CountriesID", "Name");

                            return View("AddNotes");
                        }
                    }

                    int id = addNoteRepository.AddNotes(model, Command);

                    if (id > 0)
                    {
                        ModelState.Clear();
                        ViewBag.message = "Your note has been successfully added";
                    }
                }
                else
                {
                    if (model.IsPaid)
                    {
                        if (model.SellingPrice == null)
                        {
                            ModelState.AddModelError("SellingPrice", "Selling price is required");
                        }
                    }

                    var NoteCategoryList2 = context.NoteCategories.ToList();
                    ViewBag.NotesCategory = new SelectList(NoteCategoryList2, "NoteCategoriesID", "Name");

                    var NotesTypeList2 = context.NoteTypes.ToList();
                    ViewBag.NotesType = new SelectList(NotesTypeList2, "NoteTypesID", "Name");

                    var CountryList2 = context.Countries.ToList();
                    ViewBag.NotesCountry = new SelectList(CountryList2, "CountriesID", "Name");

                    return View("AddNotes");
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

        #region Edit Note

        [HttpGet]
        public ActionResult EditNote(int id)
        {
            var user = db.Users.FirstOrDefault(x => x.EmailID == User.Identity.Name);

            var note = db.SellerNotes.Where(x => x.SellerNotesID == id && x.SellerID == user.UserID && x.Status == 6).FirstOrDefault();

            var FileNote = db.SellerNotesAttachements.Where(x => x.NoteID == note.SellerNotesID).ToList();

            var NoteCategoryList = db.NoteCategories.ToList();
            ViewBag.NotesCategory = new SelectList(NoteCategoryList, "NoteCategoriesID", "Name");

            var NotesTypeList = db.NoteTypes.ToList();
            ViewBag.NotesType = new SelectList(NotesTypeList, "NoteTypesID", "Name");

            var CountryList = db.Countries.ToList();
            ViewBag.NotesCountry = new SelectList(CountryList, "CountriesID", "Name");

            AddNotesModel editnote = new AddNotesModel();

            editnote.SellerNotesID = note.SellerNotesID;
            editnote.NoteType = note.NoteType;
            editnote.Title = note.Title;
            editnote.Category = note.Category;
            editnote.NoteType = note.NoteType;
            editnote.NumberOfPages = note.NumberOfPages;
            editnote.Description = note.Description;
            editnote.Country = note.Country;
            editnote.UniversityName = note.UniversityName;
            editnote.Course = note.Course;
            editnote.CourseCode = note.CourseCode;
            editnote.Professor = note.Professor;
            editnote.IsPaid = note.IsPaid;
            editnote.SellingPrice = note.SellingPrice;

            editnote.DisplayPicture = note.DisplayPicture;
            editnote.NotesPreview = note.NotesPreview;

            ViewBag.ImagePath = note.DisplayPicture;
            ViewBag.PreviewPath = note.NotesPreview;

            ViewBag.ImageName = Path.GetFileName(note.DisplayPicture);
            ViewBag.PreviewName = Path.GetFileName(note.NotesPreview);

            return View(editnote);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditNote(AddNotesModel editnote, string Command)
        {
            Users user = db.Users.FirstOrDefault(x => x.EmailID == User.Identity.Name);

            SellerNotes note = db.SellerNotes.FirstOrDefault(x => x.SellerNotesID == editnote.SellerNotesID && x.Status == 6 && x.SellerID == user.UserID && x.IsActive == true);

            SellerNotesAttachements AttachFile = db.SellerNotesAttachements.FirstOrDefault(x => x.NoteID == note.SellerNotesID && x.IsActive == true);

            if (user != null && ModelState.IsValid)
            {
                if (editnote.DisplayPicture != null)
                {
                    string FileNameDelete = System.IO.Path.GetFileName(note.DisplayPicture);
                    string PathImage = Request.MapPath("~/Content/NotesImages/Images/" + FileNameDelete);
                    FileInfo file = new FileInfo(PathImage);
                    if (file.Exists)
                    {
                        file.Delete();
                    }
                    string displayPictureFileName = Path.GetFileNameWithoutExtension(editnote.NoteDisplayPicturePath.FileName);
                    string displayPictureExtension = Path.GetExtension(editnote.NoteDisplayPicturePath.FileName);
                    displayPictureFileName = displayPictureFileName + DateTime.Now.ToString("yymmssff") + displayPictureExtension;
                    note.DisplayPicture = "~/Content/NotesImages/Images/" + displayPictureFileName;
                    displayPictureFileName = Path.Combine(Server.MapPath("~/Content/NotesImages/Images/"), displayPictureFileName);
                    editnote.NoteDisplayPicturePath.SaveAs(displayPictureFileName);

                    db.Configuration.ValidateOnSaveEnabled = false;
                    db.SaveChanges();
                }

                if (editnote.NotesPreview != null)
                {
                    string FileNameDelete = System.IO.Path.GetFileName(note.NotesPreview);
                    string PathPreview = Request.MapPath("~/Content/NotesImages/NotesPreview/" + FileNameDelete);
                    FileInfo file = new FileInfo(PathPreview);
                    if (file.Exists)
                    {
                        file.Delete();
                    }
                    string notePreviewFilePathFileName = Path.GetFileNameWithoutExtension(editnote.NotePreviewFilePath.FileName);
                    string notePreviewFilePathExtension = Path.GetExtension(editnote.NotePreviewFilePath.FileName);
                    notePreviewFilePathFileName = notePreviewFilePathFileName + DateTime.Now.ToString("yymmssff") + notePreviewFilePathExtension;
                    note.NotesPreview = "~/Content/NotesImages/NotesPreview/" + notePreviewFilePathFileName;
                    notePreviewFilePathFileName = Path.Combine(Server.MapPath("~/Content/NotesImages/NotesPreview/"), notePreviewFilePathFileName);
                    editnote.NotePreviewFilePath.SaveAs(notePreviewFilePathFileName);

                    db.Configuration.ValidateOnSaveEnabled = false;
                    db.SaveChanges();
                }

                if (editnote.NoteUploadFilePath != null)
                {
                    string FileNameDelete = System.IO.Path.GetFileName(editnote.FilePath);
                    string PathPreview = Request.MapPath("~/Content/NotesImages/NotesPreview/" + FileNameDelete);
                    FileInfo file = new FileInfo(PathPreview);
                    if (file.Exists)
                    {
                        file.Delete();
                    }
                    string noteUploadFilePathFileName = Path.GetFileNameWithoutExtension(editnote.NoteUploadFilePath.FileName);
                    editnote.FileName = Path.GetFileNameWithoutExtension(editnote.NoteUploadFilePath.FileName);
                    string noteUploadFilePathExtension = Path.GetExtension(editnote.NoteUploadFilePath.FileName);
                    noteUploadFilePathFileName = noteUploadFilePathFileName + DateTime.Now.ToString("yymmssff") + noteUploadFilePathExtension;
                    editnote.FilePath = "~/Content/NotesImages/NotesPDF/" + noteUploadFilePathFileName;
                    noteUploadFilePathFileName = Path.Combine(Server.MapPath("~/Content/NotesImages/NotesPDF/"), noteUploadFilePathFileName);
                    editnote.NoteUploadFilePath.SaveAs(noteUploadFilePathFileName);

                    db.Configuration.ValidateOnSaveEnabled = false;
                    db.SaveChanges();
                }

                note.Title = editnote.Title;
                note.Status = Command == "Save" ? 6 : 7;
                note.Category = editnote.Category;
                note.NoteType = editnote.NoteType;
                note.NumberOfPages = editnote.NumberOfPages;
                note.Description = editnote.Description;
                note.UniversityName = editnote.UniversityName;
                note.Country = editnote.Country;
                note.Course = editnote.Course;
                note.CourseCode = editnote.CourseCode;
                note.Professor = editnote.Professor;
                note.IsPaid = editnote.IsPaid;
                note.SellingPrice = editnote.IsPaid == false ? 0 : note.SellingPrice;
                note.ModifiedDate = DateTime.Now;

                db.Configuration.ValidateOnSaveEnabled = false;
                db.SaveChanges();

                return RedirectToAction("SellYourNotes", "Home");
            }

            return View(editnote);
        }

        #endregion Edit Note

        #region Delete Note

        [HttpPost]
        public ActionResult Delete(int id)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var currentuser = context.Users.FirstOrDefault(m => m.EmailID == User.Identity.Name).UserID;

                SellerNotes note = context.SellerNotes.SingleOrDefault(m => m.SellerNotesID == id && m.Status == 6 && m.SellerID == currentuser);

                SellerNotesAttachements noteFile = context.SellerNotesAttachements.FirstOrDefault(x => x.NoteID == note.SellerNotesID && x.IsActive == true);

                if (noteFile != null)
                {
                    string notePdfPath = Server.MapPath(noteFile.FilePath);
                    DirectoryInfo pdfPath = new DirectoryInfo(notePdfPath);
                    System.IO.File.Delete(notePdfPath);

                    context.SellerNotesAttachements.Remove(noteFile);
                    context.SaveChanges();
                }

                string notePreviewPath = Server.MapPath(note.NotesPreview);
                DirectoryInfo previewPath = new DirectoryInfo(notePreviewPath);
                System.IO.File.Delete(notePreviewPath);

                string noteImagesPath = Server.MapPath(note.DisplayPicture);
                DirectoryInfo imagesPath = new DirectoryInfo(noteImagesPath);
                System.IO.File.Delete(noteImagesPath);

                context.SellerNotes.Remove(note);
                context.SaveChanges();
            }
            return RedirectToAction("SellYourNotes");
        }

        ////Delete all file and folder in directory
        //private void EmptyFolder(DirectoryInfo directory)
        //{
        //    if (directory.GetFiles() != null)
        //    {
        //        foreach (FileInfo file in directory.GetFiles())
        //        {
        //            file.Delete();
        //        }
        //    }

        //    if (directory.GetDirectories() != null)
        //    {
        //        foreach (DirectoryInfo subdirectory in directory.GetDirectories())
        //        {
        //            EmptyFolder(subdirectory);
        //            subdirectory.Delete();
        //        }
        //    }
        //}

        #endregion Delete Note

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

        #region Note Details

        [AllowAnonymous]
        [Route("NoteDetails/{id}")]
        public ActionResult NoteDetails(int id, bool? ReadOnly)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                // default user image
                var defaultuserImg = context.SystemConfigurations.FirstOrDefault(m => m.Key == "DefaultProfileImage").Value;

                // get note details
                var notes = (from Notes in context.SellerNotes
                             join Category in context.NoteCategories on Notes.Category equals Category.NoteCategoriesID
                             let Country = context.Countries.FirstOrDefault(m => m.CountriesID == Notes.Country)
                             join Users in context.Users on Notes.SellerID equals Users.UserID
                             where Notes.SellerNotesID == id && (Notes.Status == 6 || Notes.Status == 7 || Notes.Status == 8 || Notes.Status == 9)
                             select new NoteDetailsModel
                             {
                                 ID = Notes.SellerNotesID,
                                 Title = Notes.Title,
                                 Category = Category.Name,
                                 Description = Notes.Description,
                                 Image = Notes.DisplayPicture,
                                 Price = Notes.SellingPrice,
                                 IsPaid = Notes.IsPaid,
                                 Institute = Notes.UniversityName == null ? "--" : Notes.UniversityName,
                                 Country = Country.Name == null ? "--" : Country.Name,
                                 CourseName = Notes.Course == null ? "--" : Notes.Course,
                                 CourseCode = Notes.CourseCode == null ? "--" : Notes.CourseCode,
                                 Professor = Notes.Professor == null ? "--" : Notes.Professor,
                                 Pages = (decimal)(Notes.NumberOfPages == null ? 0 : Notes.NumberOfPages),
                                 ApprovedDate = Notes.PublishedDate,
                                 NotePreview = Notes.NotesPreview,
                                 Seller = Users.FirstName + " " + Users.LastName,
                                 Status = Notes.Status
                             }).ToList();

                for (int i = 0; i < notes.Count; i++)
                {
                    notes[i].ApproveDate = notes[i].ApprovedDate.HasValue ? notes[i].ApprovedDate.GetValueOrDefault().ToString("MMMM dd yyyy") : "N/A";
                }


                // average ratings
                var avg = context.SellerNotesReviews.Where(m => m.NoteID == id).ToList();
                if (avg.Count() > 0)
                {
                    var avgReview = Math.Round(Double.Parse(avg.Average(m => m.Ratings).ToString()));
                    var count = avg.Count();
                    ViewBag.TotalReview = count;
                    ViewBag.AverageReview = avgReview;
                }
                else
                {
                    ViewBag.TotalReview = 0;
                    ViewBag.AverageReview = 0;
                }


                // Spam count
                var spam = context.SellerNotesReportedIssues.Where(m => m.NoteID == id).Count();
                ViewBag.Spam = spam;

                // customer Review List
                var reviews = (from Review in context.SellerNotesReviews
                               join User in context.Users on Review.ReviewedByID equals User.UserID
                               join UserDetail in context.UserProfile on User.UserID equals UserDetail.UserID
                               where Review.NoteID == id
                               select new CustomerReview
                               {
                                   FirstName = User.FirstName,
                                   LastName = User.LastName,
                                   Image = UserDetail.ProfilePicture == null ? defaultuserImg : UserDetail.ProfilePicture,
                                   Ratings = Review.Ratings,
                                   Review = Review.Comments
                               }).OrderByDescending(m => m.Ratings).ToList();

                ViewBag.Reviews = reviews;

                if (ReadOnly == null || ReadOnly == true)
                {
                    ViewBag.NoteDetails = notes;

                    TempData["ReadOnly"] = "true";
                    return View();
                }
                else
                {
                    ViewBag.NoteDetails = notes.Where(m => m.Status == 6).ToList();
                    return View();
                }


            }

        }

        #endregion Note Details

        #region Purchase Note

        [Route("NoteDetails/Purchase")]
        public ActionResult PurchaseNote(string noteId)
        {
            int noteid = int.Parse(noteId);

            using (var context = new NotesMarketPlaceEntities())
            {

                var user = context.Users.FirstOrDefault(m => m.EmailID == User.Identity.Name);
                var note = context.SellerNotes.FirstOrDefault(m => m.SellerNotesID == noteid);


                if (note != null && !user.Equals(null))
                {
                    var create = context.Downloads;

                    if (note.SellingPrice == 0)
                    {
                        create.Add(new Downloads
                        {
                            Downloader = user.UserID,
                            NoteID = noteid,
                            Seller = note.SellerID,
                            PurchasedPrice = note.SellingPrice,
                            //CreatedDate = DateTime.Now,
                            IsSellerHasAllowedDownload = true,
                            IsAttachmentDownloaded = true,
                            AttachmentDownloadedDate = DateTime.Now
                        });

                        context.SaveChanges();

                        var attachment = context.SellerNotesAttachements.FirstOrDefault(m => m.NoteID == noteid);

                        // download file direct
                        string filePath = Server.MapPath("../Content/NotesImages/NotesPDF/" + attachment.FileName);
                        byte[] filebyte = GetFile(filePath);

                        return File(filebyte, System.Net.Mime.MediaTypeNames.Application.Octet, attachment.FileName);
                    }
                    else
                    {
                        // send download request to seller
                        create.Add(new Downloads
                        {
                            Downloader = user.UserID,
                            NoteID = noteid,
                            Seller = note.SellerID,
                            PurchasedPrice = note.SellingPrice,
                            CreatedDate = DateTime.Now
                        });
                        context.SaveChanges();

                        // seller email
                        var seller = context.Users.FirstOrDefault(m => m.UserID == note.SellerID);

                        // send mail to seller
                        string subject = user.FirstName + " wants to purchase your notes";
                        string body = "Hello " + seller.FirstName + "\\n"
                            + "We would like to inform you that, " + user.FirstName + " wants to purchase your notes. Please see Buyer Requests tab and allow download access to Buyer if you have received the payment from him";
                        body += "\\nRegards,\\nNotes MarketPlace";

                        bool isSend = SendEmailUser.EmailSend(seller.EmailID, subject, body, false);


                        TempData["UserName"] = user.FirstName;

                        // show modal
                        TempData["ShowModal"] = 1;
                        return RedirectToAction("NoteDetails", new { id = noteId });
                    }
                }
                else
                {
                    return View("SearchNotes");
                }
            }
        }

        #endregion Purchase Note

        #region Download Note

        public ActionResult Download(int noteId, int userId)
        {
            //Download the file                                                
            var user = db.Users.FirstOrDefault(x => x.EmailID == User.Identity.Name);

            //check user enter the profile details or not
            bool temp = db.UserProfile.Any(x => x.UserID == user.UserID);
            if (!temp)
            {
                return RedirectToAction("MyProfile", "Account");
            }

            //count notes for zip or simple note download
            var note = db.SellerNotes.Find(noteId);
            var count = db.SellerNotesAttachements.Where(x => x.NoteID == noteId).Count();
            string notesattachementpath = "~/Content/NotesImages/NotesPDF/";
            if (count > 1)
            {
                var noteattachement = db.SellerNotesAttachements.Where(x => x.NoteID == note.SellerNotesID).ToList();
            }

            //full path for download file
            string filename = db.SellerNotesAttachements.FirstOrDefault(x => x.NoteID == noteId).FileName;
            string attachmentspath = notesattachementpath + filename;
            string fullpath = System.IO.Path.Combine(notesattachementpath, filename);

            //when user first time download
            if (!db.Downloads.Any(x => x.NoteID == noteId && x.Downloader == user.UserID))
            {

                //Save data in database 
                SellerNotes obj = new SellerNotes();
                NoteCategories cat = new NoteCategories();
                int category = db.SellerNotes.FirstOrDefault(x => x.SellerNotesID == noteId).Category;
                string title = db.SellerNotes.FirstOrDefault(x => x.SellerNotesID == noteId).Title;

                Downloads downloadnotedetail = new Downloads();

                downloadnotedetail.NoteID = noteId;
                downloadnotedetail.Seller = userId;
                downloadnotedetail.Downloader = user.UserID;
                downloadnotedetail.AttachmentPath = fullpath;
                downloadnotedetail.NoteTitle = title;
                downloadnotedetail.NoteCategory = db.NoteCategories.FirstOrDefault(x => x.NoteCategoriesID == category).Name;
                downloadnotedetail.CreatedDate = DateTime.Now;
                downloadnotedetail.CreatedBy = userId;
                downloadnotedetail.ModifiedDate = DateTime.Now;
                downloadnotedetail.ModifiedBy = userId;
                downloadnotedetail.AttachmentDownloadedDate = DateTime.Now;
                //downloadnotedetail.IsActive = true;


                var notes = db.SellerNotes.FirstOrDefault(x => x.SellerNotesID == noteId);
                if (notes.IsPaid)
                {
                    downloadnotedetail.IsSellerHasAllowedDownload = false;
                    downloadnotedetail.IsAttachmentDownloaded = false;
                    downloadnotedetail.IsPaid = true;
                    downloadnotedetail.PurchasedPrice = db.SellerNotes.FirstOrDefault(x => x.SellerNotesID == noteId).SellingPrice;

                    db.Downloads.Add(downloadnotedetail);
                    db.SaveChanges();

                    return RedirectToAction("NoteDetails", new { id = noteId });
                }

                downloadnotedetail.IsSellerHasAllowedDownload = true;
                downloadnotedetail.IsAttachmentDownloaded = true;
                downloadnotedetail.PurchasedPrice = 0;
                downloadnotedetail.IsPaid = false;

                db.Downloads.Add(downloadnotedetail);
                db.SaveChanges();
            }

            //check allow download
            var res = db.Downloads.FirstOrDefault(x => x.NoteID == noteId);
            if (res.IsSellerHasAllowedDownload == false && res.IsPaid == true)
            {
                int userOfNote = db.SellerNotes.FirstOrDefault(x => x.SellerNotesID == noteId).SellerID;
                string name = db.Users.FirstOrDefault(x => x.UserID == userOfNote).FirstName;
                string fullName = user.FirstName + " " + user.LastName;
                //forallowdownload(name, fullName);

                return RedirectToAction("NoteDetails", new { id = noteId });
            }

            Downloads forModifydata = new Downloads();
            forModifydata.ModifiedDate = DateTime.Now;
            forModifydata.AttachmentDownloadedDate = DateTime.Now;
            db.SaveChanges();

            //for only one file
            return File(fullpath, "text/plain", filename);
        }

        #endregion Download Note

        #region Buyer Request

        public ActionResult BuyerRequest(string txtSearch, string SortOrder, string SortBy, int PageNumber = 1)
        {
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

                ViewBag.SortOrder = SortOrder;
                ViewBag.SortBy = SortBy;
                var BuyerRequestresult = result;

                if (txtSearch != null)
                {
                    BuyerRequestresult = BuyerRequestresult.Where(x => x.NoteTitle.ToLower().Contains(txtSearch.ToLower())).ToList();

                    //Sorting
                    BuyerRequestresult = ApplySorting(SortOrder, SortBy, BuyerRequestresult);

                    //Pagination
                    BuyerRequestresult = ApplyPagination(BuyerRequestresult, PageNumber);
                }
                else
                {
                    //Sorting
                    BuyerRequestresult = ApplySorting(SortOrder, SortBy, BuyerRequestresult);

                    //Pagination
                    BuyerRequestresult = ApplyPagination(BuyerRequestresult, PageNumber);
                }

                return View(BuyerRequestresult);

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
                var result = context.Downloads.FirstOrDefault(m => m.DownloadsID == id);
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
                    string body = string.Format("Hello " + downloader.FirstName + "<br/><br/>" + "We would like to inform you that, " + seller.FirstName + " Allows you to download a note. Please login and see My Download tabs to download particular note.");
                    body += string.Format("<br/><br/> Regards,<br/><br/> Notes MarketPlace");

                    bool isSend = SendEmailUser.EmailSend(downloader.EmailID, subject, body, false);

                    System.Threading.Thread.Sleep(2000);
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                else
                {
                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                }
            }
        }

        #endregion AllowDownload

        #region My Download Notes

        public ActionResult MyDownloads(string txtSearch, string SortOrder, string SortBy, int PageNumber = 1)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                // current login userId
                int currentUser = context.Users.FirstOrDefault(m => m.EmailID == User.Identity.Name).UserID;

                var result = (from Purchase in context.Downloads
                              join Note in context.SellerNotes on Purchase.NoteID equals Note.SellerNotesID
                              join Downloader in context.Users on Purchase.Downloader equals Downloader.UserID
                              join Seller in context.Users on Purchase.Seller equals Seller.UserID
                              join UserProfile in context.UserProfile on Note.SellerID equals UserProfile.UserID
                              join Category in context.NoteCategories on Note.Category equals Category.NoteCategoriesID
                              where Purchase.IsSellerHasAllowedDownload == true && Purchase.Downloader == currentUser
                              select new MyDownloadsModel
                              {
                                  NoteId = Purchase.NoteID,
                                  UserID = currentUser,
                                  PurchaseId = Purchase.DownloadsID,
                                  Title = Note.Title,
                                  Category = Category.Name,
                                  Buyer = Downloader.EmailID,
                                  EmailID = Downloader.EmailID,
                                  Phone = UserProfile.PhoneNumber,
                                  Price = Purchase.PurchasedPrice,
                                  SellType = Purchase.PurchasedPrice == 0 ? "Free" : "Paid",
                                  DownloadDate = Purchase.AttachmentDownloadedDate
                              }).ToList();

                ViewBag.SortOrder = SortOrder;
                ViewBag.SortBy = SortBy;
                var MyDownloadsresult = result;

                if (txtSearch != null)
                {
                    MyDownloadsresult = MyDownloadsresult.Where(x => x.Title.ToLower().Contains(txtSearch.ToLower())).ToList();

                    //Sorting
                    MyDownloadsresult = ApplySorting(SortOrder, SortBy, MyDownloadsresult);

                    //Pagination
                    MyDownloadsresult = ApplyPagination(MyDownloadsresult, PageNumber);
                }
                else
                {
                    //Sorting
                    MyDownloadsresult = ApplySorting(SortOrder, SortBy, MyDownloadsresult);

                    //Pagination
                    MyDownloadsresult = ApplyPagination(MyDownloadsresult, PageNumber);
                }

                return View(MyDownloadsresult);
            }
        }

        // user review
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserReview(FormCollection form)
        {
            var user = db.Users.Where(x => x.EmailID == User.Identity.Name).FirstOrDefault();

            SellerNotesReviews reviews = new SellerNotesReviews();

            int NoteID = Convert.ToInt32(form["noteid"]);
            if (!db.SellerNotesReviews.Any(x => x.NoteID == NoteID && x.ReviewedByID == user.UserID))
            {
                reviews.NoteID = Convert.ToInt32(form["noteid"]);
                reviews.AgainstDownloadID = Convert.ToInt32(form["downloadid"]);
                reviews.ReviewedByID = user.UserID;
                reviews.Ratings = Convert.ToDecimal(form["rate"]);
                reviews.Comments = form["review"];
                reviews.CreatedDate = DateTime.Now;
                reviews.CreatedBy = user.UserID;
                reviews.IsActive = true;

                db.SellerNotesReviews.Add(reviews);
                db.SaveChanges();
            }
            else
            {
                var review = db.SellerNotesReviews.FirstOrDefault(x => x.NoteID == NoteID && x.ReviewedByID == user.UserID);
                review.AgainstDownloadID = Convert.ToInt32(form["downloadid"]);
                review.Ratings = Convert.ToDecimal(form["rate"]);
                review.Comments = form["review"];
                review.ModifiedDate = DateTime.Now;

                db.SaveChanges();
            }

            return RedirectToAction("MyDownloads");

        }

        // user report spam
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserReport(FormCollection form)
        {
            var user = db.Users.Where(x => x.EmailID == User.Identity.Name).FirstOrDefault();

            SellerNotesReportedIssues spamnote = new SellerNotesReportedIssues();

            int NoteID = Convert.ToInt32(form["noteid"]);

            if (!db.SellerNotesReportedIssues.Any(x => x.NoteID == NoteID && x.ReportedByID == user.UserID))
            {
                spamnote.NoteID = Convert.ToInt32(form["noteid"]);
                spamnote.AgainstDownloadID = Convert.ToInt32(form["downloadid"]);
                spamnote.ReportedByID = user.UserID;
                spamnote.Remarks = form["spamreport"];
                spamnote.CreatedDate = DateTime.Now;
                spamnote.CreatedBy = user.UserID;

                db.SellerNotesReportedIssues.Add(spamnote);
                db.SaveChanges();
            }
            else
            {
                var spamreport = db.SellerNotesReportedIssues.FirstOrDefault(x => x.NoteID == NoteID && x.ReportedByID == user.UserID);
                spamreport.AgainstDownloadID = Convert.ToInt32(form["downloadid"]);
                spamreport.Remarks = form["spamreport"];
                spamreport.ModifiedDate = DateTime.Now;

                db.SaveChanges();
            }
            return RedirectToAction("MyDownloads");

        }

        #endregion My Download Notes

        #region My Sold Notes

        public ActionResult MySoldNotes(string txtSearch, string SortOrder, string SortBy, int PageNumber = 1)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                // current login userId
                int currentUser = context.Users.FirstOrDefault(m => m.EmailID == User.Identity.Name).UserID;

                var result = (from Purchase in context.Downloads
                              join Note in context.SellerNotes on Purchase.NoteID equals Note.SellerNotesID
                              join Downloader in context.Users on Purchase.Downloader equals Downloader.UserID
                              //join Seller in context.Users on Purchase.Seller equals Seller.UserID
                              //join Users in context.Users on Note.SellerID equals Users.UserID
                              join UserProfile in context.UserProfile on Note.SellerID equals UserProfile.UserID
                              join Category in context.NoteCategories on Note.Category equals Category.NoteCategoriesID
                              where Purchase.IsSellerHasAllowedDownload == true && Purchase.Seller == currentUser
                              select new MySoldNotesModel
                              {
                                  Id = Purchase.DownloadsID,
                                  UserID = currentUser,
                                  NoteId = Purchase.NoteID,
                                  Title = Note.Title,
                                  Category = Category.Name,
                                  EmailID = Downloader.EmailID,
                                  Phone = UserProfile.PhoneNumber,
                                  Buyer = Downloader.EmailID,
                                  SellType = Purchase.PurchasedPrice == 0 ? "Free" : "Paid",
                                  Price = Purchase.PurchasedPrice,
                                  DownloadDate = Purchase.AttachmentDownloadedDate
                              }).ToList();

                ViewBag.SortOrder = SortOrder;
                ViewBag.SortBy = SortBy;
                var MySoldNotesresult = result;

                if (txtSearch != null)
                {
                    MySoldNotesresult = MySoldNotesresult.Where(x => x.Title.ToLower().Contains(txtSearch.ToLower())).ToList();

                    //Sorting
                    MySoldNotesresult = ApplySorting(SortOrder, SortBy, MySoldNotesresult);

                    //Pagination
                    MySoldNotesresult = ApplyPagination(MySoldNotesresult, PageNumber);
                }
                else
                {
                    //Sorting
                    MySoldNotesresult = ApplySorting(SortOrder, SortBy, MySoldNotesresult);

                    //Pagination
                    MySoldNotesresult = ApplyPagination(MySoldNotesresult, PageNumber);
                }

                return View(MySoldNotesresult);

            }
        }

        #endregion My Sold Notes

        #region My Rejected Notes

        public ActionResult MyRejectedNotes(string txtSearch, string SortOrder, string SortBy, int PageNumber = 1)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                int currentUser = context.Users.FirstOrDefault(m => m.EmailID == User.Identity.Name).UserID;

                var result = (from Notes in context.SellerNotes
                              join Status in context.ReferenceData on Notes.Status equals Status.ReferenceDataID
                              join Category in context.NoteCategories on Notes.Category equals Category.NoteCategoriesID
                              join Attachment in context.SellerNotesAttachements on Notes.SellerNotesID equals Attachment.NoteID
                              where Status.RefCategory == "Notes Status" && Status.Value == "Rejected" && Notes.SellerID == currentUser
                              select new MyRejectedNotesModel
                              {
                                  Id = Notes.SellerNotesID,
                                  UserId = Notes.SellerID,
                                  Title = Notes.Title,
                                  Category = Category.Name,
                                  Remark = Notes.AdminRemarks,
                                  DownloadNote = Attachment.FilePath
                              }).ToList();

                ViewBag.SortOrder = SortOrder;
                ViewBag.SortBy = SortBy;
                var MyRejectedNotesresult = result;

                if (txtSearch != null)
                {
                    MyRejectedNotesresult = MyRejectedNotesresult.Where(x => x.Title.ToLower().Contains(txtSearch.ToLower())).ToList();

                    //Sorting
                    MyRejectedNotesresult = ApplySorting(SortOrder, SortBy, MyRejectedNotesresult);

                    //Pagination
                    MyRejectedNotesresult = ApplyPagination(MyRejectedNotesresult, PageNumber);
                }
                else
                {
                    //Sorting
                    MyRejectedNotesresult = ApplySorting(SortOrder, SortBy, MyRejectedNotesresult);

                    //Pagination
                    MyRejectedNotesresult = ApplyPagination(MyRejectedNotesresult, PageNumber);
                }

                return View(MyRejectedNotesresult);
            }
        }

        #endregion My Rejected Notes

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
        public List<MyRejectedNotesModel> ApplySorting(string SortOrder, string SortBy, List<MyRejectedNotesModel> result)
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
                    result = result.OrderByDescending(x => x.CreatedDate).ToList();
                    break;
            }
            return result;
        }
        public List<BuyerRequestModel> ApplySorting(string SortOrder, string SortBy, List<BuyerRequestModel> result)
        {
            switch (SortBy)
            {
                case "Category":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.NoteCategory).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.NoteCategory).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.NoteCategory).ToList();
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
                                    result = result.OrderBy(x => x.NoteTitle).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.NoteTitle).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.NoteTitle).ToList();
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
        public List<MySoldNotesModel> ApplySorting(string SortOrder, string SortBy, List<MySoldNotesModel> result)
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
                    result = result.OrderByDescending(x => x.DownloadDate).ToList();
                    break;
            }
            return result;
        }
        public List<MyDownloadsModel> ApplySorting(string SortOrder, string SortBy, List<MyDownloadsModel> result)
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
                    result = result.OrderByDescending(x => x.DownloadDate).ToList();
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
        public List<MyRejectedNotesModel> ApplyPagination(List<MyRejectedNotesModel> result, int PageNumber)
        {
            ViewBag.TotalPages = Math.Ceiling(result.Count() / 5.0);
            ViewBag.PageNumber = PageNumber;

            result = result.Skip((PageNumber - 1) * 5).Take(5).ToList();

            return result;
        }
        public List<BuyerRequestModel> ApplyPagination(List<BuyerRequestModel> result, int PageNumber)
        {
            ViewBag.TotalPages = Math.Ceiling(result.Count() / 5.0);
            ViewBag.PageNumber = PageNumber;

            result = result.Skip((PageNumber - 1) * 5).Take(5).ToList();

            return result;
        }
        public List<MySoldNotesModel> ApplyPagination(List<MySoldNotesModel> result, int PageNumber)
        {
            ViewBag.TotalPages = Math.Ceiling(result.Count() / 5.0);
            ViewBag.PageNumber = PageNumber;

            result = result.Skip((PageNumber - 1) * 5).Take(5).ToList();

            return result;
        }
        public List<MyDownloadsModel> ApplyPagination(List<MyDownloadsModel> result, int PageNumber)
        {
            ViewBag.TotalPages = Math.Ceiling(result.Count() / 5.0);
            ViewBag.PageNumber = PageNumber;

            result = result.Skip((PageNumber - 1) * 5).Take(5).ToList();

            return result;
        }

        #endregion Apply Pagination

        byte[] GetFile(string s)
        {
            FileStream fs = System.IO.File.OpenRead(s);
            byte[] data = new byte[fs.Length];
            int br = fs.Read(data, 0, data.Length);
            if (br != fs.Length)
            {
                throw new IOException(s);
            }
            return data;
        }
    }
}