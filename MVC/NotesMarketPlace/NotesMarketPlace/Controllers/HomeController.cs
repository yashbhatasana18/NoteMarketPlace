using NotesMarketPlace.DB;
using NotesMarketPlace.DB.DBOperations;
using NotesMarketPlace.Email;
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
using System.Web.Security;

namespace NotesMarketPlace.Controllers
{
    [Authorize(Roles = "Admin, Super Admin, Member")]
    public class HomeController : Controller
    {
        readonly AddNotesRepository addNoteRepository = null;
        readonly SellYourNotesRepository sellYourNotesRepository = null;

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

        #region Sell Your Notes

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult SellYourNotes(string txtSearch1, string SortOrder, string SortBy, string txtSearch2, string SortOrder2, string SortBy2, int PageNumber = 1, int PageNumber2 = 1)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var currentUser = context.Users.FirstOrDefault(m => m.EmailID == User.Identity.Name);

                //Total Earning
                var Earning = (from Purchase in context.Downloads
                               where Purchase.Seller == currentUser.UserID && Purchase.IsSellerHasAllowedDownload == true
                               group Purchase by Purchase.Seller into grp
                               select grp.Sum(m => m.PurchasedPrice)).ToList();
                ViewBag.Earning = Earning.Count() == 0 ? 0 : Earning[0];

                //Total Notes Sold
                var SoldNotes = (from Purchase in context.Downloads
                                 where Purchase.Seller == currentUser.UserID && Purchase.IsSellerHasAllowedDownload == true
                                 group Purchase by Purchase.Seller into grp
                                 select grp.Count()).ToList();
                ViewBag.SoldNotes = SoldNotes.Count() == 0 ? 0 : SoldNotes[0];

                //My Download Notes
                var DownloadedNotes = (from Purchase in context.Downloads
                                       where Purchase.Downloader == currentUser.UserID && Purchase.IsSellerHasAllowedDownload == true
                                       group Purchase by Purchase.Downloader into grp
                                       select grp.Count()).ToList();
                ViewBag.DownloadNotes = DownloadedNotes.Count() == 0 ? 0 : DownloadedNotes[0];

                //My Rejected Notes
                var RejectedNotes = (from Notes in context.SellerNotes
                                     join Status in context.ReferenceData on Notes.Status equals Status.ReferenceDataID
                                     where Status.RefCategory == "Notes Status" && Status.Value == "Rejected" && Notes.SellerID == currentUser.UserID
                                     group Notes by Notes.SellerID into grp
                                     select grp.Count()).ToList();
                ViewBag.RejectedNotes = RejectedNotes.Count() == 0 ? 0 : RejectedNotes[0];

                //Buyer Requests
                var BuyerRequests = (from Purchase in context.Downloads
                                     where Purchase.IsSellerHasAllowedDownload == false && Purchase.Seller == currentUser.UserID
                                     group Purchase by Purchase.Seller into grp
                                     select grp.Count()).ToList();
                ViewBag.BuyerRequest = BuyerRequests.Count() == 0 ? 0 : BuyerRequests[0];

                //In Progress Notes
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

                //Published Notes
                var PublishedNotes = (from Notes in context.SellerNotes
                                      join Category in context.NoteCategories on Notes.Category equals Category.NoteCategoriesID
                                      join Status in context.ReferenceData on Notes.Status equals Status.ReferenceDataID
                                      where Status.RefCategory == "Notes Status" && Notes.SellerID == currentUser.UserID && Status.Value == "Published"
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

        [HttpGet]
        public ActionResult AddNotes()
        {
            ViewBag.Show = false;
            using (var context = new NotesMarketPlaceEntities())
            {
                var NoteCategoryList = context.NoteCategories.ToList();
                ViewBag.NotesCategory = new SelectList(NoteCategoryList, "NoteCategoriesID", "Name");

                var NotesTypeList = context.NoteTypes.ToList();
                ViewBag.NotesType = new SelectList(NotesTypeList, "NoteTypesID", "Name");

                var CountryList = context.Countries.ToList();
                ViewBag.NotesCountry = new SelectList(CountryList, "CountriesID", "Name");

                return View("AddNotes");
            }
        }

        [HttpPost]
        public ActionResult AddNotes(AddNotesModel model, string Command)
        {
            ViewBag.Show = false;
            using (var context = new NotesMarketPlaceEntities())
            {
                var user = context.Users.FirstOrDefault(x => x.EmailID == User.Identity.Name);
                var defaultProfileImage = context.SystemConfigurations.FirstOrDefault(m => m.Key == "DefaultProfileImage").Value;
                var defaultBookImage = context.SystemConfigurations.FirstOrDefault(m => m.Key == "DefaultBookImage").Value;

                model.SellerID = user.UserID;

                try
                {
                    if (user != null && ModelState.IsValid)
                    {
                        if (model.NoteDisplayPicturePath != null)
                        {
                            //NoteDisplayPicturePath
                            string displayPictureFileName = Path.GetFileNameWithoutExtension(model.NoteDisplayPicturePath.FileName);
                            string displayPictureExtension = Path.GetExtension(model.NoteDisplayPicturePath.FileName);
                            displayPictureFileName = displayPictureFileName + DateTime.Now.ToString("yymmssff") + displayPictureExtension;
                            model.DisplayPicture = "~/Content/NotesImages/Images/" + displayPictureFileName;
                            displayPictureFileName = Path.Combine(Server.MapPath("~/Content/NotesImages/Images/"), displayPictureFileName);
                            model.NoteDisplayPicturePath.SaveAs(displayPictureFileName);
                        }
                        else
                        {
                            model.DisplayPicture = defaultProfileImage;
                        }

                        //NoteUploadFilePath
                        string noteUploadFilePathFileName = Path.GetFileNameWithoutExtension(model.NoteUploadFilePath.FileName);
                        string noteUploadFilePathExtension = Path.GetExtension(model.NoteUploadFilePath.FileName);
                        noteUploadFilePathFileName = noteUploadFilePathFileName + DateTime.Now.ToString("yymmssff") + noteUploadFilePathExtension;
                        model.FileName = noteUploadFilePathFileName;
                        model.FilePath = "~/Content/NotesImages/NotesPDF/" + noteUploadFilePathFileName;
                        noteUploadFilePathFileName = Path.Combine(Server.MapPath("~/Content/NotesImages/NotesPDF/"), noteUploadFilePathFileName);
                        model.NoteUploadFilePath.SaveAs(noteUploadFilePathFileName);

                        if (model.NotePreviewFilePath != null)
                        {
                            //NotePreviewFilePath
                            string notePreviewFilePathFileName = Path.GetFileNameWithoutExtension(model.NotePreviewFilePath.FileName);
                            string notePreviewFilePathExtension = Path.GetExtension(model.NotePreviewFilePath.FileName);
                            notePreviewFilePathFileName = notePreviewFilePathFileName + DateTime.Now.ToString("yymmssff") + notePreviewFilePathExtension;
                            model.NotesPreview = "~/Content/NotesImages/NotesPreview/" + notePreviewFilePathFileName;
                            notePreviewFilePathFileName = Path.Combine(Server.MapPath("~/Content/NotesImages/NotesPreview/"), notePreviewFilePathFileName);
                            model.NotePreviewFilePath.SaveAs(notePreviewFilePathFileName);
                        }
                        else
                        {
                            model.NotesPreview = defaultBookImage;
                        }

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
                            ViewBag.Show = true;
                            ViewBag.AlertClass = "alert-success";
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
                }
                catch (Exception ex)
                {
                    ViewBag.Show = true;
                    ViewBag.AlertClass = "alert-danger";
                    ViewBag.message = ex.Message;
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
            ViewBag.Show = false;

            var user = db.Users.FirstOrDefault(x => x.EmailID == User.Identity.Name);

            var note = db.SellerNotes.Where(x => x.SellerNotesID == id && x.SellerID == user.UserID && x.Status == 6).FirstOrDefault();

            var FileNote = db.SellerNotesAttachements.Where(x => x.NoteID == note.SellerNotesID).ToList();

            var notesAttachements = db.SellerNotesAttachements.FirstOrDefault(x => x.NoteID == id);


            var NoteCategoryList = db.NoteCategories.ToList();
            ViewBag.NotesCategory = new SelectList(NoteCategoryList, "NoteCategoriesID", "Name");

            var NotesTypeList = db.NoteTypes.ToList();
            ViewBag.NotesType = new SelectList(NotesTypeList, "NoteTypesID", "Name");

            var CountryList = db.Countries.ToList();
            ViewBag.NotesCountry = new SelectList(CountryList, "CountriesID", "Name");

            AddNotesModel editnote = new AddNotesModel
            {
                SellerNotesID = note.SellerNotesID,
                NoteType = note.NoteType,
                Title = note.Title,
                Category = note.Category
            };
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
            editnote.FilePath = notesAttachements.FilePath;

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
            ViewBag.Show = false;

            Users user = db.Users.FirstOrDefault(x => x.EmailID == User.Identity.Name);

            SellerNotes note = db.SellerNotes.FirstOrDefault(x => x.SellerNotesID == editnote.SellerNotesID && x.Status == 6 && x.SellerID == user.UserID && x.IsActive == true);

            SellerNotesAttachements AttachFile = db.SellerNotesAttachements.FirstOrDefault(x => x.NoteID == note.SellerNotesID && x.IsActive == true);

            try
            {
                if (user != null && ModelState.IsValid)
                {
                    if (editnote.DisplayPicture != null)
                    {
                        //string FileNameDelete = System.IO.Path.GetFileName(note.DisplayPicture);
                        //string PathImage = Request.MapPath("~/Content/NotesImages/Images/" + FileNameDelete);
                        //FileInfo file = new FileInfo(PathImage);
                        //if (file.Exists)
                        //{
                        //    file.Delete();
                        //}
                        //string displayPictureFileName = Path.GetFileNameWithoutExtension(editnote.NoteDisplayPicturePath.FileName);
                        //string displayPictureExtension = Path.GetExtension(editnote.NoteDisplayPicturePath.FileName);
                        //displayPictureFileName = displayPictureFileName + DateTime.Now.ToString("yymmssff") + displayPictureExtension;
                        //note.DisplayPicture = "~/Content/NotesImages/Images/" + displayPictureFileName;
                        //displayPictureFileName = Path.Combine(Server.MapPath("~/Content/NotesImages/Images/"), displayPictureFileName);
                        //editnote.NoteDisplayPicturePath.SaveAs(displayPictureFileName);
                        note.DisplayPicture = editnote.DisplayPicture;

                        db.Configuration.ValidateOnSaveEnabled = false;
                        db.SaveChanges();
                    }

                    if (editnote.NoteDisplayPicturePath != null)
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
                        //string FileNameDelete = System.IO.Path.GetFileName(note.NotesPreview);
                        //string PathPreview = Request.MapPath("~/Content/NotesImages/NotesPreview/" + FileNameDelete);
                        //FileInfo file = new FileInfo(PathPreview);
                        //if (file.Exists)
                        //{
                        //    file.Delete();
                        //}
                        //string notePreviewFilePathFileName = Path.GetFileNameWithoutExtension(editnote.NotePreviewFilePath.FileName);
                        //string notePreviewFilePathExtension = Path.GetExtension(editnote.NotePreviewFilePath.FileName);
                        //notePreviewFilePathFileName = notePreviewFilePathFileName + DateTime.Now.ToString("yymmssff") + notePreviewFilePathExtension;
                        //note.NotesPreview = "~/Content/NotesImages/NotesPreview/" + notePreviewFilePathFileName;
                        //notePreviewFilePathFileName = Path.Combine(Server.MapPath("~/Content/NotesImages/NotesPreview/"), notePreviewFilePathFileName);
                        //editnote.NotePreviewFilePath.SaveAs(notePreviewFilePathFileName);

                        note.NotesPreview = editnote.NotesPreview;

                        db.Configuration.ValidateOnSaveEnabled = false;
                        db.SaveChanges();
                    }

                    if (editnote.NotePreviewFilePath != null)
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

                    if (editnote.FilePath != null)
                    {
                        //string FileNameDelete = System.IO.Path.GetFileName(editnote.FilePath);
                        //string PathPreview = Request.MapPath("~/Content/NotesImages/NotesPreview/" + FileNameDelete);
                        //FileInfo file = new FileInfo(PathPreview);
                        //if (file.Exists)
                        //{
                        //    file.Delete();
                        //}
                        //string noteUploadFilePathFileName = Path.GetFileNameWithoutExtension(editnote.NoteUploadFilePath.FileName);
                        //editnote.FileName = Path.GetFileNameWithoutExtension(editnote.NoteUploadFilePath.FileName);
                        //string noteUploadFilePathExtension = Path.GetExtension(editnote.NoteUploadFilePath.FileName);
                        //noteUploadFilePathFileName = noteUploadFilePathFileName + DateTime.Now.ToString("yymmssff") + noteUploadFilePathExtension;
                        //editnote.FilePath = "~/Content/NotesImages/NotesPDF/" + noteUploadFilePathFileName;
                        //noteUploadFilePathFileName = Path.Combine(Server.MapPath("~/Content/NotesImages/NotesPDF/"), noteUploadFilePathFileName);
                        //editnote.NoteUploadFilePath.SaveAs(noteUploadFilePathFileName);

                        AttachFile.FilePath = editnote.FilePath;

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
            }
            catch (Exception ex)
            {
                ViewBag.Show = true;
                ViewBag.AlertClass = "alert-danger";
                ViewBag.message = ex.Message;
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

        #endregion Delete Note

        #region Search Notes

        [AllowAnonymous]
        public ActionResult SearchNotes(int? Type, int? Category, string University, string Course, int? Country, int? Rating, string search, int PageNumber = 1)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var type = context.NoteTypes.Where(m => m.IsActive == true).ToList();
                var category = context.NoteCategories.Where(m => m.IsActive == true).ToList();
                var university = context.SellerNotes.Where(m => m.UniversityName != null).Select(x => x.UniversityName).Distinct().ToList();
                var course = context.SellerNotes.Where(m => m.Course != null).Select(x => x.Course).Distinct().ToList();
                var country = context.SellerNotes.Where(m => m.Countries != null).Select(x => x.Countries).ToList();

                var notes = (from Notes in context.SellerNotes
                             join Status in context.ReferenceData on Notes.Status equals Status.ReferenceDataID
                             where Status.Value == "Published" && Notes.IsActive == true
                             let avgRatings = (from Review in context.SellerNotesReviews
                                               where Review.NoteID == Notes.SellerNotesID
                                               group Review by Review.NoteID into grp
                                               select new SellerNotesReviewsModel
                                               {
                                                   Ratings = Math.Round(grp.Average(m => m.Ratings)),
                                                   Total = grp.Count()
                                               })
                             let spamNote = (from Spam in context.SellerNotesReportedIssues
                                             where Spam.NoteID == Notes.SellerNotesID
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

                //If Filter Value Is Available
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
                    filternotes = filternotes.Where(m => m.UniversityName.ToLower() == University.ToLower()).ToList();
                }
                if (Course != null)
                {
                    filternotes = filternotes.Where(m => m.Course.ToLower() == Course.ToLower()).ToList();
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

                ViewBag.TotalNotes = filternotes.Count();

                //Pagination
                filternotes = ApplyPagination(filternotes, PageNumber);

                return View(filternotes);
            }
        }

        #endregion Search Notes

        #region Note Details

        [AllowAnonymous]
        public ActionResult NoteDetails(int id, bool? ReadOnly)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var defaultuserImg = context.SystemConfigurations.FirstOrDefault(m => m.Key == "DefaultProfileImage").Value;

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
                                 Institute = Notes.UniversityName ?? "--",
                                 Country = Country.Name ?? "--",
                                 CourseName = Notes.Course ?? "--",
                                 CourseCode = Notes.CourseCode ?? "--",
                                 Professor = Notes.Professor ?? "--",
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

                //Average Ratings
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

                //Spam Count
                var spam = context.SellerNotesReportedIssues.Where(m => m.NoteID == id).Count();
                ViewBag.Spam = spam;

                //Customer Review List
                var reviews = (from Review in context.SellerNotesReviews
                               join User in context.Users on Review.ReviewedByID equals User.UserID
                               join UserDetail in context.UserProfile on User.UserID equals UserDetail.UserID
                               where Review.NoteID == id
                               select new CustomerReview
                               {
                                   FirstName = User.FirstName,
                                   LastName = User.LastName,
                                   Image = UserDetail.ProfilePicture ?? defaultuserImg,
                                   Ratings = Review.Ratings,
                                   Review = Review.Comments
                               }).OrderByDescending(m => m.Ratings).ToList();

                ViewBag.Reviews = reviews;

                var isDownloadAllow = context.Downloads.FirstOrDefault(m => m.NoteID == id);

                if (isDownloadAllow != null)
                {
                    ReadOnly = false;
                    ViewBag.NoteDetails = notes.Where(m => m.Status == 9).ToList();
                    return View();
                }

                if (ReadOnly == null || ReadOnly == true)
                {
                    ViewBag.NoteDetails = notes;
                    TempData["ReadOnly"] = "true";
                    return View();
                }
                {
                    ViewBag.NoteDetails = notes.Where(m => m.Status == 9).ToList();
                    return View();
                }
            }
        }

        #endregion Note Details

        #region Download Note

        public ActionResult PurchaseNote(int noteId)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                if (User.Identity.IsAuthenticated)
                {
                    var user = context.Users.FirstOrDefault(m => m.EmailID == User.Identity.Name);
                    var note = context.SellerNotes.FirstOrDefault(m => m.SellerNotesID == noteId);
                    var attachment = context.SellerNotesAttachements.FirstOrDefault(m => m.NoteID == noteId);
                    var category = context.NoteCategories.FirstOrDefault(m => m.NoteCategoriesID == note.Category);
                    var download = context.Downloads.FirstOrDefault(m => m.NoteID == noteId);


                    if (note != null && !user.Equals(null))
                    {
                        var create = context.Downloads;

                        if (note.IsPaid == false)
                        {
                            create.Add(new Downloads
                            {
                                NoteTitle = note.Title,
                                Seller = note.SellerID,
                                Downloader = user.UserID,
                                NoteID = noteId,
                                IsAttachmentDownloaded = true,
                                IsSellerHasAllowedDownload = true,
                                IsPaid = note.IsPaid,
                                AttachmentPath = attachment.FilePath,
                                NoteCategory = category.Name,
                                PurchasedPrice = note.SellingPrice,
                                CreatedDate = DateTime.Now,
                                CreatedBy = user.UserID,
                                ModifiedDate = DateTime.Now,
                                ModifiedBy = user.UserID,
                                AttachmentDownloadedDate = DateTime.Now
                            });

                            context.SaveChanges();

                            return DownloadFile(noteId);
                        }
                        else if (note.IsPaid == true)
                        {
                            if (download == null)
                            {
                                //Send Download Request To Seller
                                create.Add(new Downloads
                                {
                                    NoteTitle = note.Title,
                                    Seller = note.SellerID,
                                    Downloader = user.UserID,
                                    NoteID = noteId,
                                    IsPaid = note.IsPaid,
                                    AttachmentPath = attachment.FilePath,
                                    NoteCategory = category.Name,
                                    PurchasedPrice = note.SellingPrice,
                                    CreatedDate = DateTime.Now,
                                    CreatedBy = user.UserID,
                                    ModifiedDate = DateTime.Now,
                                    ModifiedBy = user.UserID,
                                    IsSellerHasAllowedDownload = false,
                                    IsAttachmentDownloaded = false,
                                    AttachmentDownloadedDate = DateTime.Now
                                });
                                context.SaveChanges();

                                var seller = context.Users.FirstOrDefault(m => m.UserID == note.SellerID);

                                string subject = user.FirstName + " wants to purchase your notes";
                                string body = "Hello " + seller.FirstName + ",\n \n"
                                    + "We would like to inform you that, " + user.FirstName + " wants to purchase your notes. Please see Buyer Requests tab and allow download access to Buyer if you have received the payment from him";
                                body += "\n \nRegards,\nNotes MarketPlace";

                                bool isSend = SendEmailUser.EmailSend(seller.EmailID, subject, body, false);

                                TempData["UserName"] = user.FirstName;

                                TempData["ShowModal"] = 1;

                                return RedirectToAction("NoteDetails", new { id = noteId });
                            }
                            else
                            {
                                download.ModifiedDate = DateTime.Now;
                                download.ModifiedBy = user.UserID;
                                download.IsAttachmentDownloaded = true;
                                download.AttachmentDownloadedDate = DateTime.Now;
                                context.SaveChanges();
                                return DownloadFile(noteId);
                            }
                        }
                        return View();
                    }
                    else
                    {
                        return View("SearchNotes");
                    }
                }
                else
                {
                    return RedirectToAction("Login", "Account");
                }
                //return RedirectToAction("NoteDetails", new { id = noteId });
            }
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

        [HttpPost]
        public HttpStatusCodeResult AllowDownload(int id)
        {
            if (id.Equals(null))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var context = new NotesMarketPlaceEntities())
            {
                var result = context.Downloads.FirstOrDefault(m => m.DownloadsID == id);
                var seller = context.Users.FirstOrDefault(m => m.UserID == result.Seller);
                var downloader = context.Users.FirstOrDefault(m => m.UserID == result.Downloader);

                if (result != null)
                {
                    result.IsSellerHasAllowedDownload = true;
                    context.SaveChanges();

                    string subject = seller.FirstName + " Allows you to download a note";
                    string body = string.Format("Hello " + downloader.FirstName + "\n \n" + "We would like to inform you that, " + seller.FirstName + " Allows you to download a note. Please login and see My Download tabs to download particular note.");
                    body += string.Format("\n \n Regards,\n Notes MarketPlace");

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
                int currentUser = context.Users.FirstOrDefault(m => m.EmailID == User.Identity.Name).UserID;

                var result = (from Purchase in context.Downloads
                              join Downloader in context.Users on Purchase.Downloader equals Downloader.UserID
                              join UserProfile in context.UserProfile on Purchase.Downloader equals UserProfile.UserID
                              join Category in context.NoteCategories on Purchase.NoteCategory equals Category.Name
                              where Purchase.IsSellerHasAllowedDownload == true && Purchase.Downloader == currentUser
                              select new MyDownloadsModel
                              {
                                  NoteId = Purchase.NoteID,
                                  UserID = currentUser,
                                  PurchaseId = Purchase.DownloadsID,
                                  Title = Purchase.NoteTitle,
                                  Category = Category.Name,
                                  Buyer = Downloader.EmailID,
                                  EmailID = Downloader.EmailID,
                                  Phone = UserProfile.PhoneNumber,
                                  Price = Purchase.PurchasedPrice,
                                  SellType = Purchase.PurchasedPrice == 0 ? "Free" : "Paid",
                                  DownloadDate = Purchase.AttachmentDownloadedDate
                              }).OrderByDescending(x => x.DownloadDate).ToList();

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

        //User Review
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

        //User Report Spam
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
                int currentUser = context.Users.FirstOrDefault(m => m.EmailID == User.Identity.Name).UserID;

                var result = (from Purchase in context.Downloads
                              join Note in context.SellerNotes on Purchase.NoteID equals Note.SellerNotesID
                              join Downloader in context.Users on Purchase.Downloader equals Downloader.UserID
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
                              }).OrderByDescending(x => x.DownloadDate).ToList();

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
                                  NoteId = Notes.SellerNotesID,
                                  UserId = Notes.SellerID,
                                  Title = Notes.Title,
                                  Category = Category.Name,
                                  Remark = Notes.AdminRemarks,
                                  ModifiedDate = Notes.ModifiedDate,
                                  DownloadNote = Attachment.FilePath
                              }).OrderByDescending(x => x.ModifiedDate).ToList();

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

        #region Clone Rejected Notes

        public ActionResult CloneNote(int noteId)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var currentuser = context.Users.SingleOrDefault(m => m.EmailID == User.Identity.Name).UserID;

                var oldnote = (from Note in context.SellerNotes
                               join Attachment in context.SellerNotesAttachements on Note.SellerNotesID equals Attachment.NoteID
                               where Note.SellerNotesID == noteId && Note.Status == 10 && Note.SellerID == currentuser
                               select new { Note, Attachment }).SingleOrDefault();

                oldnote.Note.Status = 11;
                oldnote.Note.IsActive = false;
                oldnote.Attachment.IsActive = false;
                oldnote.Note.PublishedDate = DateTime.Now;
                oldnote.Attachment.ModifiedDate = DateTime.Now;

                var clonenote = context.SellerNotes;
                clonenote.Add(new SellerNotes
                {
                    Title = oldnote.Note.Title,
                    Category = oldnote.Note.Category,
                    DisplayPicture = oldnote.Note.DisplayPicture,
                    NoteType = oldnote.Note.NoteType,
                    NumberOfPages = oldnote.Note.NumberOfPages,
                    Description = oldnote.Note.Description,
                    UniversityName = oldnote.Note.UniversityName,
                    Country = oldnote.Note.Country,
                    Course = oldnote.Note.Course,
                    CourseCode = oldnote.Note.CourseCode,
                    Professor = oldnote.Note.Professor,
                    SellingPrice = oldnote.Note.SellingPrice,
                    NotesPreview = oldnote.Note.NotesPreview,
                    Status = 6,
                    IsPaid = oldnote.Note.IsPaid,
                    PublishedDate = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    CreatedBy = currentuser,
                    ModifiedDate = DateTime.Now,
                    ModifiedBy = currentuser,
                    SellerID = currentuser,
                    IsActive = true
                });

                context.SaveChanges();

                var newnote = context.SellerNotes.FirstOrDefault(m => m.Status == 6 && m.Title == oldnote.Note.Title && m.SellerID == currentuser);

                if (oldnote.Note.NotesPreview != null)
                {
                    newnote.NotesPreview = oldnote.Note.NotesPreview;
                }

                var cloneattachment = context.SellerNotesAttachements;
                cloneattachment.Add(new SellerNotesAttachements
                {
                    NoteID = newnote.SellerNotesID,
                    FileName = oldnote.Attachment.FileName,
                    FilePath = oldnote.Attachment.FilePath,
                    CreatedDate = DateTime.Now,
                    CreatedBy = currentuser,
                    ModifiedDate = DateTime.Now,
                    ModifiedBy = currentuser,
                    IsActive = true
                });

                context.SaveChanges();
            }

            return RedirectToAction("SellYourNotes");
        }

        #endregion Clone Rejected Notes

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
            string Body = "Hello, \n \n" + model.Comments + "\n \n" + "Regards,\n" + model.FullName;

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
            MailMessage mail = new MailMessage
            {
                From = new MailAddress(from)
            };
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
            SmtpClient client = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                UseDefaultCredentials = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new System.Net.NetworkCredential("ynpatel2000@gmail.com", "Mahakal@18@52")
            };
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
                default:
                    result = result.OrderByDescending(x => x.AddedDate).ToList();
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
                case "Category":
                    {
                        switch (SortOrder2)
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
                        switch (SortOrder2)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.SellType).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.SellType).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.SellType).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Price":
                    {
                        switch (SortOrder2)
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
                default:
                    result = result.OrderByDescending(x => x.AddedDate).ToList();
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
                case "Remarks":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Remark).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Remark).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Remark).ToList();
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
                case "Buyer":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Downloader).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Downloader).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Downloader).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Phoneno":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Phone).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Phone).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Phone).ToList();
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
                                    result = result.OrderBy(x => x.PurchasedPrice).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.PurchasedPrice).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.PurchasedPrice).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "DownloadDate":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.AttachmentDownloadedDate).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.AttachmentDownloadedDate).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.AttachmentDownloadedDate).ToList();
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
                case "Buyer":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Buyer).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Buyer).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Buyer).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Phoneno":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Phone).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Phone).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Phone).ToList();
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
                                    result = result.OrderBy(x => x.SellType).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.SellType).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.SellType).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "DownloadDate":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.DownloadDate).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.DownloadDate).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.DownloadDate).ToList();
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
                case "Buyer":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Buyer).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Buyer).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Buyer).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Phoneno":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Phone).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Phone).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Phone).ToList();
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
                                    result = result.OrderBy(x => x.SellType).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.SellType).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.SellType).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "DownloadDate":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.DownloadDate).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.DownloadDate).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.DownloadDate).ToList();
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

        public List<SearchNotesModel> ApplyPagination(List<SearchNotesModel> result, int PageNumber)
        {
            ViewBag.TotalPages = Math.Ceiling(result.Count() / 9.0);
            ViewBag.PageNumber = PageNumber;

            result = result.Skip((PageNumber - 1) * 9).Take(9).ToList();

            return result;
        }

        #endregion Apply Pagination

        #region LogOut

        public ActionResult LogOut()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Account");
        }

        #endregion LogOut
    }
}