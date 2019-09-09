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
                    mailMessage.To.Add(new MailAddress(mails.ToLower(), null));
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

        public string SendIRFBasedClientMail(List<TransClientUser> clientUsers, string meetingDate, string jdTitle,string candidateName,string reportLink, string interviewStatus,string videoLink, string interviewerComments)
        {
            try
            {
                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(mailFrom, "Risebird Notification");
                mailMessage.To.Add(new MailAddress(mailFrom, "Risebird"));
                foreach (var clientUser in clientUsers)
                {
                    mailMessage.Bcc.Add(new MailAddress(clientUser.EmailId.ToLower(), null));
                }
                object[] parameters = new object[] { meetingDate, candidateName, jdTitle, interviewStatus, reportLink,videoLink, interviewerComments };
                mailMessage.Subject = candidateName+" Interview Completion Alert";
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "ClientMailTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);

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





        public string SendIRFBasedInterviewerMail(List<string> interviwers, string meetingDate)
        {
            try
            {                
                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(mailFrom, "Risebird Notification");
                mailMessage.To.Add(new MailAddress(mailFrom, "Risebird"));
                foreach (var mails in interviwers)
                {
                    mailMessage.Bcc.Add(new MailAddress(mails.ToLower(), null));
                }
                object[] parameters = new object[] { meetingDate };
                mailMessage.Subject = "New Interview has been scheduled on date: "+ meetingDate;
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewMailTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);
               
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
        public string SendInterviewerMailOnAssinment(string interviwers, string meetingDate)
        {
            try
            {
                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(mailFrom, "Risebird Notification");
                
                mailMessage.To.Add(new MailAddress(interviwers.ToLower(), null));
                
                object[] parameters = new object[] { meetingDate };
                mailMessage.Subject = "New Interview has been assigned on date: " + meetingDate;
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewAssingmentMailTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);

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
