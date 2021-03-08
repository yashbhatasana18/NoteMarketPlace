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

namespace NotesMarketPlace.Controllers
{
    public class AccountController : Controller
    {
        SignUpRepository repository = null;

        public AccountController()
        {
            repository = new SignUpRepository();
        }

        #region SignUp
        public ActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public ActionResult SignUp(SignUpModel model)
        {
            if (ModelState.IsValid)
            {
                int id = repository.AddUser(model);

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
        public ActionResult Login()
        {
            return View();
        }

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

        public ActionResult LogOut()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Account");
        }

        #region Mail Send
        public static void SendEmail(MailMessage mail)
        {
            SmtpClient client = new SmtpClient();
            client.Host = "smtp.gmail.com";
            client.Port = 587;
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Credentials = new System.Net.NetworkCredential("email", "password");
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

        public ActionResult Email_Verification()
        {
            return View();
        }

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
            from = "yashbhatasana1852@gmail.com";
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

        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
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
            from = "yashbhatasana1852@gmail.com";
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
    }
}