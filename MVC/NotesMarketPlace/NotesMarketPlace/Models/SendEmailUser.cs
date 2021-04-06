using NotesMarketPlace.DB;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace NotesMarketPlace.Models
{
    public class SendEmailUser
    {
        static readonly NotesMarketPlaceEntities context = new NotesMarketPlaceEntities();

        public static bool EmailSend(string senderEmail, string subject, string message, bool IsBodyHtml = false)
        {
            bool status = false;

            string supportEmail = context.SystemConfigurations.SingleOrDefault(m => m.Key == "SupportEmailAddress").Value;

            try
            {
                string HostAddress = "smtp.gmail.com";
                string FormEmailId = supportEmail;
                string Password = "Mahakal@18@52";
                string Port = "587";

                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(FormEmailId);
                mailMessage.Subject = subject;
                mailMessage.Body = message;
                mailMessage.IsBodyHtml = IsBodyHtml;
                mailMessage.To.Add(new MailAddress(senderEmail));

                SmtpClient smtp = new SmtpClient();
                smtp.Host = HostAddress;
                smtp.EnableSsl = true;

                NetworkCredential networkCredential = new NetworkCredential();
                networkCredential.UserName = mailMessage.From.Address;
                networkCredential.Password = Password;

                smtp.UseDefaultCredentials = true;
                smtp.Credentials = networkCredential;
                smtp.Port = Convert.ToInt32(Port);
                smtp.Send(mailMessage);
                status = true;

                return status;
            }
            catch (Exception e)
            {
                return status;
            }

        }
    }
}