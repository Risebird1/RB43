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

        public string SendIRFBasedClientMail(List<TransClientUser> clientUsers, string meetingDate, string jdTitle, string candidateName, string reportLink, string interviewStatus, string videoLink, string interviewerComments)
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
                object[] parameters = new object[] { meetingDate, candidateName, jdTitle, interviewStatus, reportLink, videoLink, interviewerComments };
                mailMessage.Subject = candidateName + " Interview Completion Alert";
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
                mailMessage.Subject = "New Interview has been scheduled on date: " + meetingDate;
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

        public string SendQCInterviewerMail(List<string> interviwers, string meetingDate)
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
                mailMessage.Subject = "Interview dated: " + meetingDate + " submitted for quality check.";
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "QCInterviewMailTemplate.html");
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

        public string SendAlertHRForUnassignedInterview(List<string> hrs, string meetingDate, string candidateEmail, string interviewer)
        {
            try
            {
                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(mailFrom, "Risebird Notification");
                mailMessage.To.Add(new MailAddress(mailFrom, "Risebird"));
                foreach (var mails in hrs)
                {
                    mailMessage.To.Add(new MailAddress(mails.ToLower(), null));
                }
                mailMessage.To.Add(new MailAddress("interview@risebird.io", "Interview"));
                mailMessage.To.Add(new MailAddress("kamini@risebird.io", "Job"));
                mailMessage.Subject = "HR Alert!!! " + interviewer + " Interviewer has unassigned interview for candidate " + candidateEmail + " scheduled on " + meetingDate;
                StringBuilder msgBody = new StringBuilder("Dear HR,");
                msgBody.Append("\n");
                msgBody.AppendLine(
                    "HR Alert!!! " + interviewer + " Interviewer has unassigned interview for candidate " + candidateEmail + " scheduled on " + meetingDate);
                msgBody.Append("\n");
                msgBody.AppendLine(
                    "Kindly login over risebird portal and click on Interview menu and assign interviewer for unassigned interview.");
                msgBody.Append("\n");
                msgBody.AppendLine("Note: Discuss interviewer if he/she has not informed HR before unassigning interview.");
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("Thank You, Team Risebird");

                mailMessage.Body = msgBody.ToString();
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

        public string SendCandidateInvite(string ClientName, string jdLink, List<TransClientUser> clientUsers, string primarySkill, string calendlyLink, string emailId)
        {
            try
            {
                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(mailFrom, "Interview Invite");
                mailMessage.To.Add(new MailAddress(emailId.ToLower(), "Candidate"));

                foreach (var clientUser in clientUsers)
                {
                    mailMessage.CC.Add(new MailAddress(clientUser.EmailId.ToLower(), null));
                }
                object[] parameters = new object[] { ClientName, primarySkill, jdLink, calendlyLink };
                mailMessage.Subject = ClientName + " Video Interview " + primarySkill;
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InviteCandidateTemplate.html");
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

        //CancelInviteCandidateTemplate
        public string CancelCandidateInvite(string ClientName, List<TransClientUser> clientUsers, string primarySkill, string emailId)
        {
            try
            {
                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(mailFrom, "No more vacancies!");
                mailMessage.To.Add(new MailAddress(emailId.ToLower(), "Candidate"));

                foreach (var clientUser in clientUsers)
                {
                    mailMessage.CC.Add(new MailAddress(clientUser.EmailId.ToLower(), null));
                }
                object[] parameters = new object[] { ClientName, primarySkill };
                mailMessage.Subject = ClientName + " Video Interview " + primarySkill;
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "CancelInviteCandidateTemplate.html");
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

        public string SendInterviewCancelledMail(List<string> interviewers, string interviewDate)
        {
            try
            {
                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(mailFrom, "Risebird Notification");
                mailMessage.To.Add(new MailAddress(mailFrom, "Risebird"));
                foreach (var mails in interviewers)
                {
                    mailMessage.Bcc.Add(new MailAddress(mails.ToLower(), null));
                }
                mailMessage.Subject = "Interview has been Cancelled for the date: " + interviewDate;

                StringBuilder msgBody = new StringBuilder("Dear Interviewer,");
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("You have received this communication as an interview has been cancelled on interviewdate: " + interviewDate + " .");
                msgBody.AppendLine(
                    "Thanks for your support, we will keep updating you about future interview based on your matching skill.");
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("Thank You, Team Risebird");

                mailMessage.Body = msgBody.ToString();
                SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587));
                System.Net.NetworkCredential credentials = new System.Net.NetworkCredential("azure_9119f7b456399652d5a99faa61a6fbe7@azure.com", "rise_123");
                smtpClient.Credentials = credentials;

                smtpClient.Send(mailMessage);
                return "Mail sent successfully.";
            }
            catch (Exception e)
            {
                return "Failed to send mail. Please check all filled and try again.";
            }
        }

        public string SendCandidateOnboardedMail(List<string> mailIds, string candidateName, string irfName, string evaluator)
        {
            try
            {
                //var mailsq = new List<string>();
                //mailsq.Add("harshitjajoo95@gmail.com");
                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(mailFrom, "Risebird Notification");
                mailMessage.To.Add(new MailAddress(mailFrom, "Risebird"));
                foreach (var mails in mailIds)
                {
                    mailMessage.Bcc.Add(new MailAddress(mails.ToLower(), null));
                }
                object[] parameters = new object[] { evaluator, candidateName, irfName };
                mailMessage.Subject = "New Candidate has been onboarded on: " + DateTime.Now.ToString("dd MMM yyyy");
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "ResumeEvaluationReminderTemplate.html");
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

        public string SendInterviewCancelledMail(string interviewer, string interviewDate)
        {
            try
            {
                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(mailFrom, "Risebird Notification");
                mailMessage.To.Add(new MailAddress(mailFrom, "Risebird"));
                mailMessage.To.Add(new MailAddress(interviewer, "Risebird"));
                //mailMessage.Bcc.Add(new MailAddress(interviewer, null));
                mailMessage.Subject = "Interview has been Cancelled for the date: " + interviewDate;

                StringBuilder msgBody = new StringBuilder("Dear Interviewer,");
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("You have received this communication as an interview has been cancelled for the date: " + interviewDate + " .");
                msgBody.AppendLine(
                    "Thanks for your support, we will keep updating you about future interview based on your matching skill.");
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("Thank You, Team Risebird");

                mailMessage.Body = msgBody.ToString();
                SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587));
                System.Net.NetworkCredential credentials = new System.Net.NetworkCredential("azure_9119f7b456399652d5a99faa61a6fbe7@azure.com", "rise_123");
                smtpClient.Credentials = credentials;

                smtpClient.Send(mailMessage);
                return "Mail sent successfully.";
            }
            catch (Exception e)
            {
                return "Failed to send mail. Please check all filled and try again.";
            }
        }

        public string SendInterviewCancelledMailtoCandidate(string candidateMail, string clientHRMail, string interviewDate)
        {
            try
            {
                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(mailFrom, "Risebird Notification");
                mailMessage.To.Add(new MailAddress(mailFrom, "Risebird"));
                mailMessage.To.Add(new MailAddress(candidateMail, "Risebird"));
                //mailMessage.Bcc.Add(new MailAddress(candidateMail, null));
                mailMessage.CC.Add(new MailAddress(clientHRMail, null));

                mailMessage.Subject = "Interview has been Cancelled for the date: " + interviewDate;

                StringBuilder msgBody = new StringBuilder("Dear Candidate,");
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("You have received this communication as an interview has been cancelled for the date: " + interviewDate + " .");
                msgBody.AppendLine(
                    "Thanks for your support, we will keep updating you about future interview based on your matching skill.");
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("Thank You, Team Risebird");

                mailMessage.Body = msgBody.ToString();
                SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587));
                System.Net.NetworkCredential credentials = new System.Net.NetworkCredential("azure_9119f7b456399652d5a99faa61a6fbe7@azure.com", "rise_123");
                smtpClient.Credentials = credentials;

                smtpClient.Send(mailMessage);
                return "Mail sent successfully.";
            }
            catch (Exception e)
            {
                return "Failed to send mail. Please check all filled and try again.";
            }
        }
        public string CuretatedNotification(Mail model, string UserType, string EmailParameter)
        {
            try
            {
                var mailMessage = new MailMessage();

                mailMessage.From = new MailAddress(mailFrom, "Risebird Notification");
                var toMails = model.TO.Split(";");
                foreach (var mails in toMails)
                {
                    mailMessage.To.Add(new MailAddress(mails.ToLower(), null));
                }
                object[] parameters = new object[] { UserType, EmailParameter };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "EmailNotification.html");
                string msgBody = string.Format(rawEmail, parameters);

                mailMessage.Subject = model.Subject;
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

        public string SendCuratedUserMail(List<string> curatedUsers, string meetingDate, string irf, string primarySkill, string codedName)
        {
            try
            {
                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(mailFrom, "Risebird Notification");
                mailMessage.To.Add(new MailAddress(mailFrom, "Risebird"));
                foreach (var mails in curatedUsers)
                {
                    mailMessage.Bcc.Add(new MailAddress(mails.ToLower(), null));
                }
                object[] parameters = new object[] { irf, primarySkill, codedName, meetingDate };
                mailMessage.Subject = "New candidate selected!";
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "CuratedUserSelectedMail.html");
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

        public string SharedVideoNofitication(Mail model, string UrlDetails,string skills)
        {
            try
            {
                var mailMessage = new MailMessage();
                var UserType = "Dear";
                mailMessage.From = new MailAddress(mailFrom, "Risebird Notification");
                mailMessage.CC.Add(new MailAddress(model.CC.ToLower(), null));
                var toMails = model.TO.Split(";");
                foreach (var mails in toMails)
                {
                    mailMessage.To.Add(new MailAddress(mails.ToLower(), null));
                }
                object[] parameters = new object[] { UrlDetails, skills };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InternalUserSharingTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);

                mailMessage.Subject = model.Subject;
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
        public string SharedVideoNofiticationExternal(Mail model, string UrlDetails, string url)
        {
            try
            {
                var mailMessage = new MailMessage();
                var UserType = "Dear";
                mailMessage.From = new MailAddress(mailFrom, "Risebird Notification");
                mailMessage.CC.Add(new MailAddress(model.CC.ToLower(), null));
                var toMails = model.TO.Split(";");
                foreach (var mails in toMails)
                {
                    mailMessage.To.Add(new MailAddress(mails.ToLower(), null));
                }
                object[] parameters = new object[] { UrlDetails, url };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "ExternalUserSharingTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);

                mailMessage.Subject = model.Subject;
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
