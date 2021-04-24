using NotesMarketPlace.DB;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace NotesMarketPlace.Email
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

                MailMessage mailMessage = new MailMessage
                {
                    From = new MailAddress(FormEmailId),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = IsBodyHtml
                };
                mailMessage.To.Add(new MailAddress(senderEmail));

                SmtpClient smtp = new SmtpClient
                {
                    Host = HostAddress,
                    EnableSsl = true
                };

                NetworkCredential networkCredential = new NetworkCredential
                {
                    UserName = mailMessage.From.Address,
                    Password = Password
                };

                smtp.UseDefaultCredentials = true;
                smtp.Credentials = networkCredential;
                smtp.Port = Convert.ToInt32(Port);
                smtp.Send(mailMessage);
                status = true;

                return status;
            }
            catch (Exception ex)
            {
                return status;
            }

        }
    }
}