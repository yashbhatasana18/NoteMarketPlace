using NotesMarketPlace.DB;
using NotesMarketPlace.DB.DBOperations;
using NotesMarketPlace.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web.Mvc;

namespace NotesMarketPlace.Controllers
{
    public class HomeController : Controller
    {
        AddNotesRepository addNoteRepository = null;

        public HomeController()
        {
            addNoteRepository = new AddNotesRepository();
        }

        public ActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Member")]
        public ActionResult SellYourNotes()
        {
            return View();
        }

        //public ActionResult SearchNotes()
        //{
        //    var result = repository.GetNotes();
        //    using (var context = new NotesMarketPlaceEntities())
        //    {
        //        ViewBag.Category = new SelectList(context.NoteCategories, "Id", "Name");
        //    }
        //    return View(result);
        //}

        // get search note
        [AllowAnonymous]
        [Route("SearchNotes")]
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
                             //let avgRatings = (from Review in _Context.SellerNotesReviews
                             //                  where Review.NoteID == Notes.SellerID
                             //                  group Review by Review.NoteID into grp
                             //                  select new AvgRatings
                             //                  {
                             //                      Rating = Math.Round(grp.Average(m => m.Rating)),
                             //                      Total = grp.Count()
                             //                  })
                             //let spamNote = (from Spam in _Context.Spam_Notes
                             //                where Spam.NoteId == Notes.Id
                             //                group Spam by Spam.NoteId into grp
                             //                select new SpamNote
                             //                {
                             //                    Total = grp.Count()
                             //                })
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
                                 NotesPreview = Notes.NotesPreview
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
                //if (!Rating.Equals(null))
                //{
                //    filternotes = filternotes.Where(m => m.Rating >= Rating).ToList();
                //}
                if (search != null)
                {
                    filternotes = filternotes.Where(m => m.Title.ToLower().Contains(search.ToLower())).ToList();
                }


                return View(filternotes);
            }


        }

        //[HttpPost]
        //public ActionResult SearchNotes(string NotesTitle)
        //{
        //    var result = repository.GetNotes(NotesTitle);
        //    return View(result);
        //}

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

                //ViewBag.NotesCategory = context.NoteCategories.Where(m => m.IsActive == true).Select(c => new SelectListItem { Value = c.NoteCategoriesID.ToString(), Text = c.Name }).ToList();
                ViewBag.NotesType = context.NoteTypes.Where(m => m.IsActive == true).Select(c => new SelectListItem { Value = c.NoteTypesID.ToString(), Text = c.Name }).ToList();
                ViewBag.Country = context.Countries.Where(m => m.IsActive == true).Select(c => new SelectListItem { Value = c.CountriesID.ToString(), Text = c.Name }).ToList();

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
                //Users user = new Users();
                //if (ModelState.IsValid)
                //{
                //var result = Session["Result"];
                
                int id = addNoteRepository.AddNotes(model);

                if (id > 0)
                {
                    ModelState.Clear();
                    ViewBag.message = "Your note has been successfully added";
                }
                //}
                var NoteCategoryList = context.NoteCategories.ToList();
                ViewBag.NotesCategory = new SelectList(NoteCategoryList, "NoteCategoriesID", "Name");

                //ViewBag.NotesCategory = context.NoteCategories.Where(m => m.IsActive == true).Select(c => new SelectListItem { Value = c.NoteCategoriesID.ToString(), Text = c.Name }).ToList();
                ViewBag.NotesType = context.NoteTypes.Where(m => m.IsActive == true).Select(c => new SelectListItem { Value = c.NoteTypesID.ToString(), Text = c.Name }).ToList();
                ViewBag.Country = context.Countries.Where(m => m.IsActive == true).Select(c => new SelectListItem { Value = c.CountriesID.ToString(), Text = c.Name }).ToList();

                return View("AddNotes");
            }
        }
        #endregion  AddNote
    }
}