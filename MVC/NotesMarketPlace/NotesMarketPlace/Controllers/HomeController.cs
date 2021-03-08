using System;
using NotesMarketPlace.DB;
using NotesMarketPlace.DB.DBOperations;
using NotesMarketPlace.Models;
using System.Net.Mail;
using System.Text;
using System.Web.Mvc;

namespace NotesMarketPlace.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
        public ActionResult SellYourNotes()
        {
            return View();
        }

        [Authorize]
        public ActionResult AddNotes()
        {
            return View();
        }


        public ActionResult SearchNotes()
        {
            return View();
        }

        [Authorize]
        public ActionResult BuyerRequest()
        {
            return View();
        }

        public ActionResult Faq()
        {
            return View();
        }

        #region ContactUs

        public ActionResult ContactUs()
        {
            return View();
        }

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
    }
}