using System.Linq;
using System.Web.Mvc;
using NotesMarketPlace.DB;
using NotesMarketPlace.DB.DBOperations;
using NotesMarketPlace.Models;
using System.Web.Security;
using System.Web.Hosting;
using System.Text;
using System.Net.Mail;
using System;
using System.Drawing;
using System.IO;
using System.Web.Routing;

namespace NotesMarketPlace.Controllers
{
    [Authorize(Roles = "Admin, Super Admin, Member")]
    public class AccountController : Controller
    {
        SignUpRepository signUpRepository = null;

        #region Default Constructor
        public AccountController()
        {
            signUpRepository = new SignUpRepository();

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

        #region User CRUD
        public ActionResult GetAllUsers()
        {
            var result = signUpRepository.GetAllUser();
            return View(result);
        }

        public ActionResult GetUsers(int id)
        {
            var result = signUpRepository.GetUser(id);
            return View(result);
        }

        public ActionResult EditUsers(int id)
        {
            var result = signUpRepository.GetUser(id);
            return View(result);
        }

        [HttpPost]
        public ActionResult EditUsers(SignUpModel model)
        {
            if (ModelState.IsValid)
            {
                signUpRepository.EditUsers(model.UserID, model);
                return RedirectToAction("GetAllUsers");
            }
            return View(model);
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            signUpRepository.DeleteUsers(id);
            return RedirectToAction("GetAllUsers");
        }
        #endregion User CRUD

        #region SignUp
        [AllowAnonymous]
        public ActionResult SignUp()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult SignUp(SignUpModel model)
        {
            if (ModelState.IsValid)
            {
                int id = signUpRepository.AddUser(model);

                if (id > 0)
                {
                    ModelState.Clear();
                    ViewBag.message = "Your account has been successfully created.Check your mail for activation.";
                }

                BuildEmailTemplate(id);
            }
            return View();
        }
        #endregion SignUp

        #region Login
        [AllowAnonymous]
        public ActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Login(LoginModel model)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                bool isValid = context.Users.Any(x => x.EmailID == model.EmailID && x.Password == model.Password);

                if (isValid)
                {
                    FormsAuthentication.SetAuthCookie(model.EmailID, false);
                    return RedirectToAction("SearchNotes", "Home");
                }
                ModelState.AddModelError("", "Invalid EmailID And Password.");
                return View();
            }
        }
        #endregion Login

        #region User Profile

        // get my Profile
        public ActionResult UserProfile()
        {
            using (var _Context = new NotesMarketPlaceEntities())
            {
                // get gender for dropdown
                var gender = _Context.ReferenceData.Where(m => m.RefCategory == "Gender").ToList();
                // get country
                var country = _Context.Countries.ToList();

                // get current userId
                var currentuser = _Context.Users.FirstOrDefault(m => m.EmailID == User.Identity.Name);

                // get user details
                var isDetailsAvailable = _Context.UserProfile.FirstOrDefault(m => m.UserID == currentuser.UserID);


                var UserProfile = new UserProfileModel();

                // check user details available or not
                if (isDetailsAvailable != null)
                {
                    UserProfile = (from Detail in _Context.UserProfile
                                   join User in _Context.Users on Detail.UserID equals User.UserID
                                   join Country in _Context.UserProfile on Detail.Country equals Country.Country
                                   where Detail.UserID == currentuser.UserID
                                   select new UserProfileModel
                                   {
                                       FirstName = User.FirstName,
                                       LastName = User.LastName,
                                       EmailID = User.EmailID,
                                       Gender = Detail.Gender,
                                       DOB = Detail.DOB,
                                       PhoneNumberCountryCode = Detail.PhoneNumberCountryCode,
                                       PhoneNumber = Detail.PhoneNumber,
                                       ProfilePicture = Detail.ProfilePicture,
                                       AddressLine1 = Detail.AddressLine1,
                                       AddressLine2 = Detail.AddressLine2,
                                       City = Detail.City,
                                       State = Detail.State,
                                       ZipCode = Detail.ZipCode,
                                       Country = Detail.Country,
                                       University = Detail.University,
                                       College = Detail.College
                                   }).FirstOrDefault<UserProfileModel>();

                    var GenderList = gender;
                    ViewBag.GenderList = new SelectList(GenderList, "ReferenceDataID", "Value");

                    var CountryList = country;
                    ViewBag.CountryList = new SelectList(CountryList, "CountriesID", "Name");

                    var CountryCodeList = country;
                    ViewBag.CountryCodeList = new SelectList(CountryCodeList, "CountriesID", "CountryCode");

                    if (UserProfile.ProfilePicture == null)
                    {
                        UserProfile.ProfilePicture = "~/Content/images/upload-file.png";
                    }

                    //UserProfile.genderModel = gender.Select(x => new ReferenceDataModel { ReferenceDataID = x.ReferenceDataID, Value = x.Value }).ToList();
                    //UserProfile.countryModel = country.Select(x => new CountriesModel { CountriesID = x.CountriesID, Name = x.Name }).ToList();
                    //UserProfile.CountryCodeModel = country.Select(x => new CountriesModel { CountriesID = x.CountriesID, CountryCode = x.CountryCode }).ToList();

                    return View(UserProfile);
                }
                // if user is first time login
                else
                {
                    UserProfile.FirstName = currentuser.FirstName;
                    UserProfile.LastName = currentuser.LastName;
                    UserProfile.EmailID = currentuser.EmailID;

                    var GenderList = gender;
                    ViewBag.GenderList = new SelectList(GenderList, "ReferenceDataID", "Value");

                    var CountryList = country;
                    ViewBag.CountryList = new SelectList(CountryList, "CountriesID", "Name");

                    var CountryCodeList = country;
                    ViewBag.CountryCodeList = new SelectList(CountryCodeList, "CountriesID", "CountryCode");

                    if (UserProfile.ProfilePicture == null)
                    {
                        UserProfile.ProfilePicture = "~/Content/images/upload-file.png";
                    }

                    //UserProfile.genderModel = gender.Select(x => new ReferenceDataModel { ReferenceDataID = x.ReferenceDataID, Value = x.Value }).ToList();
                    //UserProfile.countryModel = country.Select(x => new CountriesModel { CountriesID = x.CountriesID, Name = x.Name }).ToList();
                    //UserProfile.CountryCodeModel = country.Select(x => new CountriesModel { CountriesID = x.CountriesID, CountryCode = x.CountryCode }).ToList();

                    return View(UserProfile);
                }
            }
        }

        // Update my Profile
        [HttpPost]
        public ActionResult UserProfile(UserProfileModel user)
        {
            using (var _Context = new NotesMarketPlaceEntities())
            {
                // get gender for dropdown
                var gender = _Context.ReferenceData.Where(m => m.RefCategory == "Gender").ToList();
                // get country
                var country = _Context.Countries.ToList();

                if (ModelState.IsValid)
                {
                    // get current userId
                    int currentuser = _Context.Users.FirstOrDefault(m => m.EmailID == User.Identity.Name).UserID;

                    // get user details
                    var isDetailsAvailable = _Context.UserProfile.FirstOrDefault(m => m.UserID == currentuser);

                    // check user details available or not
                    if (isDetailsAvailable != null && user != null)
                    {
                        // update details
                        var userUpdate = _Context.Users.FirstOrDefault(m => m.UserID == currentuser);
                        var detailsUpdate = _Context.UserProfile.FirstOrDefault(m => m.UserID == currentuser);

                        userUpdate.FirstName = user.FirstName;
                        userUpdate.LastName = user.LastName;
                        userUpdate.EmailID = user.EmailID;
                        userUpdate.ModifiedDate = DateTime.Now;

                        _Context.Entry(userUpdate).State = System.Data.Entity.EntityState.Modified;
                        _Context.SaveChanges();

                        CreateDirectory(currentuser);

                        if (user.UserProfilePicturePath != null)
                        {
                            //UserProfilePicturePath
                            string userProfilePicturePathFileName = Path.GetFileNameWithoutExtension(user.UserProfilePicturePath.FileName);
                            string userProfilePicturePathExtension = Path.GetExtension(user.UserProfilePicturePath.FileName);
                            userProfilePicturePathFileName = userProfilePicturePathFileName + DateTime.Now.ToString("yymmssff") + userProfilePicturePathExtension;
                            user.ProfilePicture = "~/Members/" + currentuser + "/" + userProfilePicturePathFileName;
                            userProfilePicturePathFileName = Path.Combine(Server.MapPath("~/Members/" + currentuser + "/"), userProfilePicturePathFileName);
                            user.UserProfilePicturePath.SaveAs(userProfilePicturePathFileName);
                        }

                        if (user.ProfilePicture == null || user.ProfilePicture == "~/Content/images/upload-file.png")
                        {
                            user.ProfilePicture = null;
                        }

                        detailsUpdate.DOB = user.DOB;
                        detailsUpdate.Gender = user.Gender;
                        detailsUpdate.PhoneNumberCountryCode = user.PhoneNumberCountryCode;
                        detailsUpdate.PhoneNumber = user.PhoneNumber;
                        detailsUpdate.ProfilePicture = user.ProfilePicture;
                        detailsUpdate.AddressLine1 = user.AddressLine1;
                        detailsUpdate.AddressLine2 = user.AddressLine2;
                        detailsUpdate.City = user.City;
                        detailsUpdate.State = user.State;
                        detailsUpdate.ZipCode = user.ZipCode;
                        detailsUpdate.Country = user.Country;
                        detailsUpdate.College = user.College;
                        detailsUpdate.University = user.University;
                        detailsUpdate.ModifiedBy = currentuser;
                        detailsUpdate.ModifiedDate = DateTime.Now;

                        _Context.Entry(detailsUpdate).State = System.Data.Entity.EntityState.Modified;
                        _Context.SaveChanges();
                        var id = detailsUpdate.UserProfileID;
                        if (id > 0)
                        {
                            //user.ProfilePicture = "~/Content/images/upload-file.png";
                            ModelState.Clear();
                            ViewBag.message = "Your Profile Updated Successfully.";
                        }
                        //return RedirectToAction("UserProfile");
                    }
                    else
                    {
                        CreateDirectory(currentuser);

                        if (user.UserProfilePicturePath != null)
                        {
                            //UserProfilePicturePath
                            string userProfilePicturePathFileName = Path.GetFileNameWithoutExtension(user.UserProfilePicturePath.FileName);
                            string userProfilePicturePathExtension = Path.GetExtension(user.UserProfilePicturePath.FileName);
                            userProfilePicturePathFileName = userProfilePicturePathFileName + DateTime.Now.ToString("yymmssff") + userProfilePicturePathExtension;
                            user.ProfilePicture = "~/Members/" + currentuser + "/" + userProfilePicturePathFileName;
                            userProfilePicturePathFileName = Path.Combine(Server.MapPath("~/Members/" + currentuser + "/"), userProfilePicturePathFileName);
                            user.UserProfilePicturePath.SaveAs(userProfilePicturePathFileName);
                        }

                        if (user.ProfilePicture == null || user.ProfilePicture == "~/Content/images/upload-file.png")
                        {
                            user.ProfilePicture = null;
                        }

                        // create new details
                        UserProfile userProfile = new UserProfile()
                        {
                            UserID = currentuser,
                            DOB = user.DOB,
                            Gender = user.Gender,
                            PhoneNumberCountryCode = user.PhoneNumberCountryCode,
                            PhoneNumber = user.PhoneNumber,
                            ProfilePicture = user.ProfilePicture,
                            AddressLine1 = user.AddressLine1,
                            AddressLine2 = user.AddressLine2,
                            City = user.City,
                            State = user.State,
                            ZipCode = user.ZipCode,
                            Country = user.Country,
                            University = user.University,
                            College = user.College,
                            CreatedDate = DateTime.Now,
                            CreatedBy = currentuser,
                            ModifiedDate = DateTime.Now,
                            ModifiedBy = currentuser
                        };

                        _Context.UserProfile.Add(userProfile);

                        _Context.SaveChanges();
                        var id = userProfile.UserProfileID;
                        //return RedirectToAction("UserProfile");
                        if (id > 0)
                        {
                            ModelState.Clear();
                            //userProfile.ProfilePicture = "~/Content/images/upload-file.png";
                            ViewBag.message = "Your Profile Updated Successfully.";
                        }
                    }
                }

                //user.ProfilePicture = "~/Content/images/upload-file.png";

                var GenderList = gender;
                ViewBag.GenderList = new SelectList(GenderList, "ReferenceDataID", "Value");

                var CountryList = country;
                ViewBag.CountryList = new SelectList(CountryList, "CountriesID", "Name");

                var CountryCodeList = country;
                ViewBag.CountryCodeList = new SelectList(CountryCodeList, "CountriesID", "CountryCode");
            }

            return View();
        }

        #endregion User Profile

        #region Mail Send
        public static void SendEmail(MailMessage mail)
        {
            SmtpClient client = new SmtpClient();
            client.Host = "smtp.gmail.com";
            client.Port = 587;
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Credentials = new System.Net.NetworkCredential("ynpatel2000@gmail.com", "Mahakal@18@52");
            try
            {
                client.Send(mail);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion Mail Send

        #region Random Password
        public string RandomPassword()
        {
            string numbers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz!@#$%^&*()-=";
            Random objrandom = new Random();
            string passwordString = "";
            string strrandom = string.Empty;
            for (int i = 0; i < 8; i++)
            {
                int temp = objrandom.Next(0, numbers.Length);
                passwordString = numbers.ToCharArray()[temp].ToString();
                strrandom += passwordString;
            }
            var strongpwd = strrandom;
            return strongpwd;
        }
        #endregion Random Password

        #region Account Activation

        [AllowAnonymous]
        public ActionResult Email_Verification()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult VerifyEmail(int userID)
        {
            ViewBag.userID = userID;
            return View();
        }

        public JsonResult RegisterConfirm(int userID)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                Users Data = context.Users.FirstOrDefault(x => x.UserID == userID);
                Data.IsEmailVerified = true;
                Data.IsActive = true;
                context.SaveChanges();
                var msg = "Your Email Is Verified!";
                return Json(msg, JsonRequestBehavior.AllowGet);
            }
        }

        public void BuildEmailTemplate(int userID)
        {
            string body = System.IO.File.ReadAllText(HostingEnvironment.MapPath("~/Views/EmailTemplate/") + "AccountConfirmation" + ".html");
            using (var context = new NotesMarketPlaceEntities())
            {
                var regInfo = context.Users.FirstOrDefault(x => x.UserID == userID);
                var url = "https://localhost:44380/" + "Account/VerifyEmail?userID=" + userID;
                body = body.Replace("{Username}", regInfo.FirstName);
                body = body.Replace("{ConfirmationLink}", url);
                body = body.ToString();
                BuildEmailTemplate("Note Marketplace - Email Verification", body, regInfo.EmailID);
            }
        }

        public static void BuildEmailTemplate(string subjectText, string bodyText, string sendTo)
        {
            string from, to, bcc, cc, subject, body;
            from = "ynpatel2000@gmail.com";
            to = sendTo.Trim();
            bcc = "";
            cc = "";
            subject = subjectText;
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

        #endregion Account Activation

        #region ForgotPassword

        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult ForgotPassword(SignUpModel model)
        {
            BuildForgotPasswordTemplate(model.EmailID);
            return View();
        }

        public void BuildForgotPasswordTemplate(string emailID)
        {
            string body = System.IO.File.ReadAllText(HostingEnvironment.MapPath("~/Views/EmailTemplate/") + "Forgot_Password" + ".cshtml");
            using (var context = new NotesMarketPlaceEntities())
            {
                var regInfo = context.Users.FirstOrDefault(x => x.EmailID == emailID);
                if (regInfo != null)
                {
                    var rendomPassword = RandomPassword();
                    regInfo.Password = rendomPassword;
                    body = body.Replace("@ViewBag.Password", rendomPassword);
                    body = body.Replace("@ViewBag.UserName", regInfo.FirstName);

                    ViewBag.ForgotPassword = "Password Has Been Mailed You.";
                }
                context.SaveChanges();
                BuildForgotPasswordTemplate("New Temporary Password has been created for you", body, regInfo.EmailID);
            }
        }

        public static void BuildForgotPasswordTemplate(string subjectText, string bodyText, string sendTo)
        {
            string from, to, bcc, cc, subject, body;
            from = "ynpatel2000@gmail.com";
            to = sendTo.Trim();
            bcc = "";
            cc = "";
            subject = subjectText;
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

        #endregion ForgotPassword

        #region Change Password
        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                if (ModelState.IsValid)
                {
                    // get current user
                    var currentUser = context.Users.FirstOrDefault(m => m.EmailID == User.Identity.Name);

                    // old password not match
                    if (!currentUser.Password.Equals(model.OldPassword))
                    {
                        TempData["Oldpwd"] = "1";
                        return View();
                    }

                    // new pwd & conf-pwd not match
                    if (!model.NewPassword.Equals(model.ConfirmPassword))
                    {
                        return View();
                    }

                    // old pwd & new pwd same
                    if (currentUser.Password == model.ConfirmPassword)
                    {
                        TempData["OldpwdSame"] = "1";
                        return View();
                    }

                    // update password
                    currentUser.Password = model.ConfirmPassword;
                    currentUser.ModifiedDate = DateTime.Now;
                    context.SaveChanges();

                    FormsAuthentication.SignOut();

                    return RedirectToAction("Login", "Account");
                }
            }
            return View();
        }
        #endregion Change Password

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

        public ActionResult LogOut()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Account");
        }
    }
}