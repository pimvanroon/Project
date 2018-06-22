using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PcaIdentityService.Models;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace PcaIdentityService.Internal_Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger logger;

        public EmailSender(IOptions<EmailSettings> emailSettings, ILogger<EmailSender> logger)
        {
            EmailSettings = emailSettings.Value;
            this.logger = logger;
        }

        public EmailSettings EmailSettings { get; }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            return Execute(email, subject, message);
        }

        public async Task Execute(string email, string subject, string message)
        {
            try
            {
                MailMessage mail = new MailMessage()
                {
                    From = new MailAddress(EmailSettings.PrimaryUsernameEmail, "PcaVault")
                };
                mail.To.Add(new MailAddress(email));

                mail.Subject = "PeterConnects - " + subject;
                mail.Body = message;
                mail.IsBodyHtml = true;
                mail.Priority = MailPriority.Normal;

                using (SmtpClient smtp = new SmtpClient(EmailSettings.PrimaryDomain, EmailSettings.PrimaryPort))
                {
                    smtp.Credentials = new NetworkCredential(EmailSettings.PrimaryUsernameEmail, EmailSettings.PrimaryUsernamePassword);
                    smtp.EnableSsl = true;
                    await smtp.SendMailAsync(mail);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("something went wrong " + ex);
            }
        }
    }
}
