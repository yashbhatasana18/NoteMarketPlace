using NotesMarketPlace.DB;
using NotesMarketPlace.DB.DBOperations;
using NotesMarketPlace.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web.Mvc;

namespace NotesMarketPlace.Controllers
{
    public class HomeController : Controller
    {
        AddNotesRepository addNoteRepository = null;
        SellYourNotesRepository sellYourNotesRepository = null;

        public HomeController()
        {
            addNoteRepository = new AddNotesRepository();
            sellYourNotesRepository = new SellYourNotesRepository();
        }

        public ActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Member")]
        public ActionResult BuyerRequest()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult Faq()
        {
            return View();
        }

        #region Sell Your Notes

        [Authorize(Roles = "Member")]
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult SellYourNotes(string txtSearch1, string SortOrder, string SortBy, int PageNumber = 1)
        {
            ViewBag.SortOrder = SortOrder;
            ViewBag.SortBy = SortBy;

            var result = sellYourNotesRepository.GetAllPublishedNotes();

            if (txtSearch1 != null)
            {
                result = result.Where(x => x.Title.ToLower().Contains(txtSearch1.ToLower())).ToList();

                //Sorting
                result = ApplySorting(SortOrder, SortBy, result);

                //Pagination
                result = ApplyPagination(result, PageNumber);
            }
            else
            {
                //Sorting
                result = ApplySorting(SortOrder, SortBy, result);

                //Pagination
                result = ApplyPagination(result, PageNumber);
            }

            return View(result);
        }

        #endregion Sell Your Notes

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

        #region AddNote

        [Authorize(Roles = "Member")]
        [HttpGet]
        public ActionResult AddNotes()
        {

            using (NotesMarketPlaceEntities context = new NotesMarketPlaceEntities())
            {

                var NoteCategoryList = context.NoteCategories.ToList();
                ViewBag.NotesCategory = new SelectList(NoteCategoryList, "NoteCategoriesID", "Name");

                var NotesTypeList = context.NoteTypes.ToList();
                ViewBag.NotesType = new SelectList(NotesTypeList, "NoteTypesID", "Name");

                var CountryList = context.Countries.ToList();
                ViewBag.NotesCountry = new SelectList(CountryList, "CountriesID", "Name");

                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
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
            using (var _Context = new NotesMarketPlaceEntities())
            {
                // get all types
                var type = _Context.NoteTypes.Where(m => m.IsActive == true).ToList();
                // get all category
                var category = _Context.NoteCategories.Where(m => m.IsActive == true).ToList();
                // get distinct university
                var university = _Context.SellerNotes.Where(m => m.UniversityName != null).Select(x => x.UniversityName).Distinct().ToList();
                // get distinct courses
                var course = _Context.SellerNotes.Where(m => m.Course != null).Select(x => x.Course).Distinct().ToList();
                // get all countries
                var country = _Context.SellerNotes.Where(m => m.Countries != null).Select(x => x.Countries).Distinct().ToList();

                // get all book details
                var notes = (from Notes in _Context.SellerNotes
                             join Status in _Context.ReferenceData on Notes.Status equals Status.ReferenceDataID
                             where Status.Value == "Published" && Notes.IsActive == true
                             let avgRatings = (from Review in _Context.SellerNotesReviews
                                               where Review.NoteID == Notes.SellerID
                                               group Review by Review.NoteID into grp
                                               select new SellerNotesReviewsModel
                                               {
                                                   Ratings = Math.Round(grp.Average(m => m.Ratings)),
                                                   Total = grp.Count()
                                               })
                             let spamNote = (from Spam in _Context.SellerNotesReportedIssues
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

        #region Apply Sorting

        public List<SellerNotesModel> ApplySorting(string SortOrder, string SortBy, List<SellerNotesModel> result)
        {
            switch (SortBy)
            {
                case "Added Date":
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
                    result = result.OrderBy(x => x.CreatedDate).ToList();
                    break;
            }
            return result;
        }

        #endregion Apply Sorting

        #region Apply Pagination

        public List<SellerNotesModel> ApplyPagination(List<SellerNotesModel> result, int PageNumber)
        {
            ViewBag.TotalPages = Math.Ceiling(result.Count() / 5.0);
            ViewBag.PageNumber = PageNumber;

            result = result.Skip((PageNumber - 1) * 5).Take(5).ToList();

            return result;
        }

        #endregion Apply Pagination
    }
}