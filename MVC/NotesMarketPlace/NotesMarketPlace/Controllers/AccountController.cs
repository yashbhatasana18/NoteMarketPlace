using NotesMarketPlace.DB;
using NotesMarketPlace.DB.DBOperations;
using NotesMarketPlace.Models;
using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;

namespace NotesMarketPlace.Controllers
{
    [Authorize(Roles = "Admin, Super Admin, Member")]
    public class AccountController : Controller
    {
        readonly SignUpRepository signUpRepository = null;

        #region Default Constructor
        public AccountController()
        {
            signUpRepository = new SignUpRepository();

            using (var context = new NotesMarketPlaceEntities())
            {
                // social URL
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
                    ViewBag.Color = "rgb(14, 185, 14)";
                    ViewBag.message = "Your account has been successfully created.Check your mail for activation.";
                    BuildEmailTemplate(id);
                }
                else
                {
                    ViewBag.Color = "rgb(255, 0, 0)";
                    ViewBag.message = "This Email Address Already Exist.Please Enter Other One.";
                }
            }
            return View();
        }

        #endregion SignUp

        #region Login

        [AllowAnonymous]
        public ActionResult Login()
        {
            ViewBag.Show = false;
            //HttpCookie cookie = Request.Cookies["LoginModel"];
            //if (cookie != null)
            //{
            //    ViewBag.emailID = cookie["emailID"].ToString();

            //    string EncryptedPassword = cookie["pwd"].ToString();
            //    byte[] b = Convert.FromBase64String(EncryptedPassword);
            //    string DecryptPassword = ASCIIEncoding.ASCII.GetString(b);

            //    ViewBag.pwd = DecryptPassword.ToString();
            //}
            TempData["WrongPassword"] = "";
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Login(LoginModel model)
        {
            ViewBag.Show = false;
            using (var context = new NotesMarketPlaceEntities())
            {
                if (!ModelState.IsValid)
                {
                    return RedirectToAction("Login", "Account");
                }

                var isValid = context.Users.Where(x => x.EmailID == model.EmailID).FirstOrDefault();

                if (isValid == null)
                {
                    TempData["WrongPassword"] = "";
                    ViewBag.Show = true;
                    ViewBag.Color = "rgb(255, 0, 0)";
                    ViewBag.Message = "Your EmailID Is Invalid.";
                    //return RedirectToAction("Login", "Account");
                    return View("Login");
                }

                if (isValid.IsEmailVerified == false)
                {
                    TempData["WrongPassword"] = "";
                    ViewBag.Show = true;
                    ViewBag.Color = "rgb(255, 0, 0)";
                    ViewBag.message = "Please Verify Your Account. We Sended Email Verify Link In Your EmailID.";
                    //return RedirectToAction("Login", "Account");
                    return View("Login");
                }

                if (isValid.IsActive == false)
                {
                    TempData["WrongPassword"] = "";
                    ViewBag.Show = true;
                    ViewBag.Color = "rgb(255, 0, 0)";
                    ViewBag.Message = "Your Account Has Been InActive. Please Contact Admin For Activation.";
                    //return RedirectToAction("Login", "Account");
                    return View("Login");
                }

                //HttpCookie cookie = new HttpCookie("LoginModel");
                if (model.RememberMe == true)
                {
                    //cookie["emailID"] = model.EmailID;

                    //byte[] b = ASCIIEncoding.ASCII.GetBytes(model.Password);
                    //string EncryptedPassword = Convert.ToBase64String(b);

                    //cookie["pwd"] = EncryptedPassword;
                    //cookie.Expires = DateTime.Now.AddDays(2);
                    //HttpContext.Response.Cookies.Add(cookie);
                    FormsAuthentication.SetAuthCookie(isValid.EmailID, true);
                }
                else
                {
                    //cookie.Expires = DateTime.Now.AddDays(-1);
                    //HttpContext.Response.Cookies.Add(cookie);
                    FormsAuthentication.SetAuthCookie(isValid.EmailID, false);
                }

                if (isValid.Password.Equals(model.Password))
                {
                    // user is Member
                    if (isValid.RoleID == 3)
                    {
                        // if first time login
                        var firsttime = context.UserProfile.FirstOrDefault(m => m.UserID == isValid.UserID);

                        if (firsttime == null)
                        {
                            //Session["emailID"] = model.EmailID;
                            return RedirectToAction("UserProfile", "Account");
                        }
                        else
                        {
                            //Session["emailID"] = model.EmailID;
                            return RedirectToAction("SearchNotes", "Home");
                        }
                    }
                    // user is Admin
                    else
                    {
                        //Session["emailID"] = model.EmailID;
                        return RedirectToAction("Dashboard", "Admin");
                    }

                }
                else
                {
                    TempData["WrongPassword"] = "Wrong Password";
                    return View("Login");
                }
            }
        }

        #endregion Login

        #region User Profile

        public ActionResult UserProfile()
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var gender = context.ReferenceData.Where(m => m.RefCategory == "Gender").ToList();
                var country = context.Countries.ToList();
                var currentuser = context.Users.FirstOrDefault(m => m.EmailID == User.Identity.Name);
                var isDetailsAvailable = context.UserProfile.FirstOrDefault(m => m.UserID == currentuser.UserID);
                var UserProfile = new UserProfileModel();

                //If User Details Are Availabel
                if (isDetailsAvailable != null)
                {
                    UserProfile = (from Detail in context.UserProfile
                                   join User in context.Users on Detail.UserID equals User.UserID
                                   join Country in context.UserProfile on Detail.Country equals Country.Country
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
                        ViewBag.ProfilePicture = "~/Content/images/upload-file.png";
                        ViewBag.ProfilePicturePreview = "#";
                        ViewBag.HideClass = "";
                        ViewBag.NonHideClass = "hidden";
                        ViewBag.ProfilePictureName = "";
                    }
                    else
                    {
                        ViewBag.ProfilePicture = "~/Content/images/upload-file.png";
                        ViewBag.ProfilePicturePreview = UserProfile.ProfilePicture;
                        ViewBag.ProfilePictureName = Path.GetFileNameWithoutExtension(UserProfile.ProfilePicture);
                        ViewBag.HideClass = "hidden";
                        ViewBag.NonHideClass = "";
                    }
                    return View(UserProfile);
                }
                //If User Is First Time Login
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

                    ViewBag.ProfilePicture = "~/Content/images/upload-file.png";
                    ViewBag.ProfilePicturePreview = "#";
                    ViewBag.HideClass = "";
                    ViewBag.NonHideClass = "hidden";
                    ViewBag.ProfilePictureName = "";

                    return View(UserProfile);
                }
            }
        }

        [HttpPost]
        public ActionResult UserProfile(UserProfileModel user)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var gender = context.ReferenceData.Where(m => m.RefCategory == "Gender").ToList();
                var country = context.Countries.ToList();

                if (ModelState.IsValid)
                {
                    int currentuser = context.Users.FirstOrDefault(m => m.EmailID == User.Identity.Name).UserID;
                    var isDetailsAvailable = context.UserProfile.FirstOrDefault(m => m.UserID == currentuser);
                    var systemConfiguration = context.SystemConfigurations.FirstOrDefault(m => m.Key == "DefaultProfileImage").Value;

                    //Check User Details Available Or Not
                    if (isDetailsAvailable != null && user != null)
                    {
                        //Update Details
                        var userUpdate = context.Users.FirstOrDefault(m => m.UserID == currentuser);
                        var detailsUpdate = context.UserProfile.FirstOrDefault(m => m.UserID == currentuser);

                        userUpdate.FirstName = user.FirstName;
                        userUpdate.LastName = user.LastName;
                        userUpdate.EmailID = user.EmailID;
                        userUpdate.ModifiedDate = DateTime.Now;

                        context.Entry(userUpdate).State = System.Data.Entity.EntityState.Modified;
                        context.SaveChanges();

                        CreateDirectory(currentuser);

                        if (user.UserProfilePicturePath != null)
                        {
                            string FileNameDelete = System.IO.Path.GetFileName(user.ProfilePicture);
                            string PathPreview = Request.MapPath("~/Members/" + currentuser + "/" + FileNameDelete);
                            FileInfo file = new FileInfo(PathPreview);
                            if (file.Exists)
                            {
                                file.Delete();
                            }
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
                            user.ProfilePicture = systemConfiguration.ToString();
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

                        context.Entry(detailsUpdate).State = System.Data.Entity.EntityState.Modified;
                        context.SaveChanges();
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
                            user.ProfilePicture = systemConfiguration;
                        }

                        //Create New Details
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

                        context.UserProfile.Add(userProfile);

                        context.SaveChanges();
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

                ViewBag.ProfilePicture = "~/Content/images/upload-file.png";
                ViewBag.ProfilePicturePreview = "#";
                ViewBag.HideClass = "";
                ViewBag.NonHideClass = "hidden";
                ViewBag.ProfilePictureName = "";

                //user.ProfilePicture = "~/Content/images/upload-file.png";

                var GenderList = gender;
                ViewBag.GenderList = new SelectList(GenderList, "ReferenceDataID", "Value");

                var CountryList = country;
                ViewBag.CountryList = new SelectList(CountryList, "CountriesID", "Name");

                var CountryCodeList = country;
                ViewBag.CountryCodeList = new SelectList(CountryCodeList, "CountriesID", "CountryCode");
            }

            return RedirectToAction("SearchNotes", "Home");
        }

        #endregion User Profile

        #region Mail Send

        [HandleError]
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
                throw new Exception(ex.Message);
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

        [AllowAnonymous]
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

        #endregion Account Activation

        #region ForgotPassword

        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            ViewBag.Show = false;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult ForgotPassword(SignUpModel model)
        {
            ViewBag.Show = false;
            using (var context = new NotesMarketPlaceEntities())
            {
                var users = context.Users.Where(m => m.EmailID == model.EmailID).Count();

                if (users != 0)
                {
                    BuildForgotPasswordTemplate(model.EmailID);

                }
                else
                {
                    ViewBag.Show = true;
                    ViewBag.Color = "rgb(255, 0, 0)";
                    ViewBag.message = "This EmailID is not registered with us.";
                }
                return View();
            }
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

        #endregion ForgotPassword

        #region Change Password

        public ActionResult ChangePassword()
        {
            ViewBag.Show = false;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            ViewBag.Show = false;
            using (var context = new NotesMarketPlaceEntities())
            {
                if (ModelState.IsValid)
                {
                    var currentUser = context.Users.FirstOrDefault(m => m.EmailID == User.Identity.Name);

                    //Old Password Not Match
                    if (!currentUser.Password.Equals(model.OldPassword))
                    {
                        ViewBag.Show = true;
                        ViewBag.Color = "rgb(255, 0, 0)";
                        ViewBag.message = "Please Enter Correct Old Password.";
                        return View();
                    }

                    //New Password & Confirm-Password Not Match
                    if (!model.NewPassword.Equals(model.ConfirmPassword))
                    {
                        return View();
                    }

                    //Old Password & New Password Same
                    if (currentUser.Password == model.ConfirmPassword)
                    {
                        return View();
                    }

                    //Update Password
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
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Account");
        }

        #endregion LogOut
    }
}