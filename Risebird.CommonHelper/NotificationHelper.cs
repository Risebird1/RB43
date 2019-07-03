using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class NotificationHelper : INotificationHelper
    {
        private readonly IApplicationErrorLogger _errorLogger;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly string sendGridUserName = string.Empty;
        private readonly string sendGridPassword = string.Empty;
        private readonly string mailFrom = string.Empty;
        private readonly IConfiguration _configuration;

        public NotificationHelper(IAzureStorageHelper azureStorageHelper, IConfiguration configuration)
        {
            //_errorLogger = errorLogger;
            _configuration = configuration;
            _azureStorageHelper = azureStorageHelper;
            sendGridUserName = _configuration["SendGridKeySettings:UserName"];
            sendGridPassword = _configuration["SendGridKeySettings:Password"];
            mailFrom = _configuration["SendGridKeySettings:MailFrom"];
        }

        public string SendHelpsupportMail(Mail model)
        {
            try
            {
                object[] parameters = new object[] { model.Body };
                var mailMessage = new MailMessage();

                mailMessage.From = new MailAddress(mailFrom, "Risebird Notification");
                var toMails = model.TO.Split(";");
                foreach (var mails in toMails)
                {
                    mailMessage.To.Add(new MailAddress(mails, null));
                }

                mailMessage.Subject = model.Subject;
                string msgBody = string.Format(model.Body);
                mailMessage.IsBodyHtml = true;
                mailMessage.Body = msgBody;
                SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587));
                System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(sendGridUserName, sendGridPassword);
                smtpClient.Credentials = credentials;

                smtpClient.Send(mailMessage);
                return "Mail sent successfully.";
            }
            catch (Exception e)
            {
                return "Failed to send mail. Please check all filled and try again.";
            }
        }

    }
}
