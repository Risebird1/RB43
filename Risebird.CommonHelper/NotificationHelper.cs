using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class NotificationHelper : INotificationHelper
    {
        private readonly IApplicationErrorLogger _errorLogger;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly ISendGridRepository _sendGridRepository;
        private readonly string sendGridUserName = string.Empty;
        private readonly string sendGridPassword = string.Empty;
        private readonly string mailFrom = string.Empty;
        private readonly IConfiguration _configuration;
        private readonly string _sendGridClientKey = string.Empty;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public NotificationHelper(IAzureStorageHelper azureStorageHelper, IConfiguration configuration, IApplicationErrorLogger errorLogger, ISendGridRepository sendGridRepository, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _azureStorageHelper = azureStorageHelper;
            sendGridUserName = _configuration["SendGridKeySettings:UserName"];
            sendGridPassword = _configuration["SendGridKeySettings:Password"];
            mailFrom = _configuration["SendGridKeySettings:MailFrom"];
            _sendGridClientKey = _configuration["SendGridKeySettings:ApiKey"];
            _errorLogger = errorLogger;
            _sendGridRepository = sendGridRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> SendHelpsupportMail(Mail model)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Support Notification");
                msg.Subject = model.Subject;

                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }

                object[] parameters = new object[] { model.Body };

                string msgBody = string.Format(model.Body);
                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        //public string SendIRFBasedClientMail(List<TransClientUser> clientUsers, string meetingDate, string jdTitle, string candidateName, string reportLink, string interviewStatus, string videoLink, string interviewerComments)
        //{
        //    try
        //    {
        //        var mailMessage = new MailMessage();
        //        mailMessage.From = new MailAddress(mailFrom, "Risebird Notification");
        //        mailMessage.To.Add(new MailAddress(mailFrom, "Risebird"));
        //        foreach (var clientUser in clientUsers)
        //        {
        //            mailMessage.Bcc.Add(new MailAddress(clientUser.EmailId.ToLower(), null));
        //        }
        //        object[] parameters = new object[] { meetingDate, candidateName, jdTitle, interviewStatus, reportLink, videoLink, interviewerComments };
        //        mailMessage.Subject = candidateName + " Interview Completion Alert";
        //        string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "ClientMailTemplate.html");
        //        string msgBody = string.Format(rawEmail, parameters);

        //        mailMessage.IsBodyHtml = true;
        //        mailMessage.Body = msgBody;
        //        SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587));
        //        System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(sendGridUserName, sendGridPassword);
        //        smtpClient.Credentials = credentials;

        //        smtpClient.Send(mailMessage);
        //        return "Mail sent successfully.";
        //    }
        //    catch (Exception)
        //    {
        //        return "Failed to send mail. Please check all fields and try again.";
        //    }
        //}


        public async Task<string> SendIRFBasedInterviewerMail(List<string> interviwers, string meetingDate, int? irfId = null, int? interviewId = null)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Urgent");
                msg.Subject = "New Interview has been scheduled on date: " + meetingDate;

                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird"));
                foreach (var mails in interviwers)
                {
                    msg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(mails, null));
                }

                object[] parameters = new object[] { meetingDate };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "EmergencyInterviewNotification.html");//"InterviewMailTemplate.html"
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg, irfId, interviewId);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        //public async Task<string> SendQCInterviewerMail(List<string> interviwers, string meetingDate)
        //{
        //    try
        //    {
        //        var msg = new SendGridMessage();
        //        msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird QC Alert");
        //        msg.Subject = "Interview dated: " + meetingDate + " submitted for quality check.";

        //        msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird"));
        //        foreach (var mails in interviwers)
        //        {
        //            msg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(mails, null));
        //        }

        //        object[] parameters = new object[] { meetingDate };
        //        string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "QCInterviewMailTemplate.html");
        //        string msgBody = string.Format(rawEmail, parameters);
        //        msg.HtmlContent = msgBody;

        //        return await SendMail(msg);
        //    }
        //    catch (Exception)
        //    {
        //        return "Failed to send mail. Please check all fields and try again.";
        //    }
        //}

        //public string SendAlertHRForUnassignedInterview(List<string> hrs, string meetingDate, string candidateEmail, string interviewer, string Skillchannelid, string Client)
        //{
        //    try
        //    {

        //        var msg = new SendGridMessage();
        //        msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Unassigned Interview");
        //        msg.Subject = "HR Alert!!! " + interviewer + " Interviewer has unassigned interview for candidate " + candidateEmail + " scheduled on " + meetingDate;

        //        foreach (var mail in hrs)
        //        {
        //            msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
        //        }
        //        msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress("prasad_srinivas@risebird.io", "Prasad Srinivas"));
        //        msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress("shweta_kadechkar@risebird.io", "Shweta Kadechkar"));
        //        msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress("soumyashree_gudi@risebird.io", "Soumyashree Gudi"));

        //        StringBuilder msgBody = new StringBuilder("Dear HR,");
        //        msgBody.Append("\n");
        //        msgBody.AppendLine(
        //            "HR Alert!!! " + interviewer + " Interviewer has unassigned interview for candidate " + candidateEmail + " scheduled on " + meetingDate + " for Client: " + Client + "(" + Skillchannelid + ").");
        //        msgBody.Append("\n");
        //        msgBody.AppendLine(
        //            "Kindly login over risebird portal and click on Interview menu and assign interviewer for unassigned interview.");
        //        msgBody.Append("\n");
        //        msgBody.AppendLine("Note: Discuss interviewer if he/she has not informed HR before unassigning interview.");
        //        msgBody.Append("\n");
        //        msgBody.Append("\n");
        //        msgBody.AppendLine("Thank You, Team Risebird");

        //        msg.HtmlContent = msgBody.ToString();

        //        return SendMail(msg).Result;
        //    }
        //    catch (Exception)
        //    {
        //        return "Failed to send mail. Please check all fields and try again.";
        //    }
        //}
        public async Task<string> SendInterviewerMailOnAssinment(string interviwers, string meetingDate)
        {
            try
            {

                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Interview Notification");
                msg.Subject = "New Interview has been assigned on date: " + meetingDate;
                //msg.Subject = "New Interview has been assigned on date: " + DateTime.Now.ToString();

                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(interviwers, null));
                //msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress("harshit_jajoo@risebird.io", null));

                object[] parameters = new object[] { meetingDate };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewAssingmentMailTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);
                //string msgBody = "You have been assigned with a new interview.";


                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public async Task<string> SendCandidateInvite(string ClientName, string jdLink, List<TransClientUser> clientUsers, string primarySkill, string calendlyLink, string emailId, string candidateName, string clientHREmail, string staffingAgency = null, int? irfId = null)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Interview Invite");
                msg.Subject = ClientName + " - Video Interview - " + primarySkill;
                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(emailId, emailId.Split("@").First()));
                if (clientUsers != null && clientUsers.Count > 0)
                {
                    foreach (var clientUser in clientUsers)
                    {
                        msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(clientUser.EmailId, clientUser.EmailId.Split("@").First()));
                    }
                    if (!string.IsNullOrEmpty(staffingAgency) && !clientUsers.Any(x => x.EmailId == staffingAgency))
                    {
                        msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(staffingAgency, staffingAgency.Split("@").First()));
                    }
                }
                else if (!string.IsNullOrEmpty(clientHREmail))
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(clientHREmail, clientHREmail.Split("@").First()));
                }

                object[] parameters = new object[] { candidateName, ClientName, primarySkill, jdLink };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InviteCandidateTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg, irfId);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        //CancelInviteCandidateTemplate
        public async Task<string> CancelCandidateInvite(string ClientName, List<TransClientUser> clientUsers, string primarySkill, string emailId, int? irfId = null)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "No more vacancies!");
                msg.Subject = ClientName + " Video Interview " + primarySkill;

                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(emailId.ToLower(), "Candidate"));
                foreach (var clientUser in clientUsers)
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(clientUser.EmailId, null));
                }

                object[] parameters = new object[] { ClientName, primarySkill };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "CancelInviteCandidateTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;

                return await SendMail(msg, irfId);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public async Task<string> SendInterviewCancelledMail(List<string> interviewers, string interviewDate, List<string> irfOwnersAndRestrictedIrfOwners = null, int? irfId = null, int? interviewId = null)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Interview Cancel");
                msg.Subject = "Interview has been Cancelled for the date: " + interviewDate;

                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird"));
                foreach (var mail in interviewers)
                {
                    msg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                StringBuilder msgBody = new StringBuilder("Dear Interviewer,");
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("You have received this communication as an interview has been cancelled on interview date: " + interviewDate + " .");
                msgBody.AppendLine(
                    "Thanks for your support, we will keep updating you about future interview based on your matching skill.");
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("Thank You, Team Risebird");

                msg.HtmlContent = msgBody.ToString();

                return await SendMail(msg, irfId, interviewId);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public async Task<string> SendCandidateOnboardedMail(List<string> mailIds, string candidateName, string irfName, string evaluator)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = "New Candidate has been onboarded on: " + DateTime.Now.ToString("dd MMM yyyy");

                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird"));
                foreach (var mail in mailIds)
                {
                    msg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }

                object[] parameters = new object[] { evaluator, candidateName, irfName };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "ResumeEvaluationReminderTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public async Task<string> SendInterviewCancelledMail(string interviewer, string interviewDate, string IRFid, int? irfId = null)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Interview Cancel");
                msg.Subject = "Interview has been Cancelled for the date: " + interviewDate;
                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird"));
                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(interviewer, "Risebird"));


                StringBuilder msgBody = new StringBuilder("Dear Interviewer,");
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("You have received this communication as an interview has been cancelled for the date: " + interviewDate + " . And SkillChannel Id: " + IRFid);
                msgBody.AppendLine(
                    "Thanks for your support, we will keep updating you about future interview based on your matching skill.");
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("Thank You, Team Risebird");

                msg.HtmlContent = msgBody.ToString();

                return await SendMail(msg, irfId);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public async Task<string> SendInterviewCancelledMailtoCandidate(string candidateMail, string clientHRMail, string interviewDate, int? irfId = null)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Interview Cancel");
                msg.Subject = "Interview has been Cancelled for the date: " + interviewDate;
                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird"));
                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(candidateMail, "Risebird"));

                StringBuilder msgBody = new StringBuilder("Dear Candidate,");
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("You have received this communication as an interview has been cancelled for the date: " + interviewDate + " .");
                msgBody.AppendLine(
                    "Thanks for your support, we will keep updating you about future interview based on your matching skill.");
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("Thank You, Team Risebird");

                msg.HtmlContent = msgBody.ToString();

                return await SendMail(msg, irfId);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> CuretatedNotification(Mail model, string UserType, string EmailParameter)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));

                }

                object[] parameters = new object[] { UserType, EmailParameter };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "EmailNotification.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public async Task<string> SendCuratedUserMail(List<string> curatedUsers, string meetingDate, string irf, string primarySkill, string codedName)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = "New candidate selected!";
                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird"));

                foreach (var mail in curatedUsers)
                {
                    msg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(mail, null));

                }

                object[] parameters = new object[] { irf, primarySkill, codedName, meetingDate };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "CuratedUserSelectedMail.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public async Task<string> SharedVideoNofitication(Mail model, string UrlDetails, string skills)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;

                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));

                }
                msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(model.CC, null));


                object[] parameters = new object[] { UrlDetails, skills };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InternalUserSharingTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SharedVideoNofiticationExternal(Mail model, string UrlDetails, string url)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;

                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));

                }
                msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(model.CC, null));


                object[] parameters = new object[] { UrlDetails, url };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "ExternalUserSharingTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SharedVideoNofiticationExternal(Mail model, string name, string url, string comment, string resumeLink)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;

                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));

                }
                msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(model.CC, null));


                var anchrTag = "<a href=" + url + ">Please Click to see the video</a>";
                object[] parameters = new object[] { name, anchrTag, comment, resumeLink };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "ExternalUserSharingTemplateNew.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendBroadcastMail(List<string> receivers, List<string> supportEmails, BroadcastModel model)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;

                foreach (var supportEmail in supportEmails)
                {
                    if (!string.IsNullOrEmpty(supportEmail))
                    {
                        msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(supportEmail, null));
                    }
                }
                foreach (var email in receivers)
                {
                    msg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(email, null));

                }

                object[] parameters = new object[] { model.BroadcastReciever, model.Message };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "BroadcastTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;


                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Broadcast failed.";
            }
        }
        public async Task<string> SendInviteToInterviewer(string iscName, string jdTitle, string primarySkill, string calendlyLink, string interviewerMailId, string hrEmailId)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Discussion Invite");
                msg.Subject = "Discussion Invitation(" + primarySkill + ")";
                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(interviewerMailId, "Interviewer"));
                msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(hrEmailId, null));



                object[] parameters = new object[] { primarySkill, calendlyLink };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InviteInterviewerTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public async Task<string> CancelInterviewerInvite(string iscName, string primarySkill, string interviewerMailId, string hrEmailId)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Discussion cancelled!");
                msg.Subject = "Discussion Cancelled";
                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(interviewerMailId, "Interviewer"));
                msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(hrEmailId, null));


                object[] parameters = new object[] { iscName, primarySkill };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "CancelInviteInterviewerTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public async Task<string> SendCommonPoolMail(List<string> clientUsers, string clientEmailId, Candidate candidateDetails)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = "New Candidate added to Common Pool";
                foreach (var email in clientUsers)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(email, null));

                }

                msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(clientEmailId, null));



                object[] parameters = new object[] { candidateDetails.Name, (candidateDetails.Skill != null ? candidateDetails.Skill : string.Empty) };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "CommonPoolTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed";
            }
        }
        public async Task<string> MeetingInvite(DateTime startTime, DateTime endTime, string TimeZone, string accountMail, string accountPass, string Message, string subject, string sender, string candidateMail, string additionalMail, string meetingURL)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = $"Confidential: Team Meeting Account detail for meeting schedule at {startTime} with {candidateMail}";
                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(sender, null));

                object[] parameters = new object[] { accountMail, accountPass };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "MeetingAccountDetail.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);

            }
            catch (Exception)
            {
                return "Failed! Could not send the Email.";
            }
        }
        public async Task<string> VideoAccessRequest(Mail model, string url, string Emailid)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }


                var anchrTag = "<a href=" + url + ">Candidate Interview Video</a>";
                object[] parameters = new object[] { Emailid, anchrTag };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "RequestForAccess.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> ShowIntrestInterview(Mail model, string Email, string videoURL)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(model.CC, null));

                object[] parameters = new object[] { Email, "<a href=" + videoURL + ">" };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewInterestShown.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;


                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendAssessmentNotification(Mail model, string emailid)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }

                object[] parameters = new object[] { emailid };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "AssessmentNotification.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendAssessmentNotificationComments(Mail model, string assessment, string comments)
        {
            try
            {

                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }


                object[] parameters = new object[] { assessment, comments };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "AssessmentNotificationComment.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public async Task<string> SendVmStopReminder(VMActionPerformed obj)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = "Virtual Machine Running!";
                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(obj.PerformedBy, "Risebird"));


                object[] parameters = new object[] { obj.Name };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "StopVMNotifier.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public async Task<string> CandidateJDScoreNotification(Mail model, int candidateCount, string JDName)
        {
            try
            {

                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(model.CC, null));


                object[] parameters = new object[] { candidateCount, JDName };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "CandidateJDScoreNotification.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SurveyNotification(Mail model, string UrlDetails)
        {
            try
            {

                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }


                object[] parameters = new object[] { UrlDetails };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "SurveyNotification.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> FeedbackSubmitNotification(Mail model, string candidate)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }

                object[] parameters = new object[] { candidate };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "FeedbackSubmitNotification.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> InsuficientBalancNotifiaction(Mail model)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }

                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InsuficientBalanceNotification.html");
                string msgBody = string.Format(rawEmail);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SharedTestLink(Mail model, string title, string timeduration, string url, string ClientName, string meetingurl, DateTime interviewdatetime, int offset)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }

                object[] parameters = new object[] { title, timeduration, url, ClientName, meetingurl };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "CandidateInvitationTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                //Start: Attachment
                StringBuilder str = new StringBuilder();
                str.AppendLine("BEGIN:VCALENDAR");
                str.AppendLine("PRODID:-//Schedule a Meeting");
                str.AppendLine("VERSION:2.0");
                str.AppendLine("METHOD:REQUEST");
                str.AppendLine("BEGIN:VEVENT");
                str.AppendLine(string.Format("DTSTART:{0:yyyyMMddTHHmmssZ}", interviewdatetime.AddMinutes(offset)));
                str.AppendLine(string.Format("DTSTAMP:{0:yyyyMMddTHHmmssZ}", DateTime.UtcNow));
                str.AppendLine(string.Format("DTEND:{0:yyyyMMddTHHmmssZ}", interviewdatetime.AddMinutes(offset).AddMinutes(60)));
                str.AppendLine("LOCATION:" + meetingurl);
                str.AppendLine(string.Format("UID:{0}", Guid.NewGuid()));
                str.AppendLine(string.Format("SUMMARY:{0}", msg.Subject));
                str.AppendLine(string.Format("ATTENDEE;CN=\"{0}\";RSVP=TRUE:mailto:{1}", toMails[0], toMails[0]));
                str.AppendLine("BEGIN:VALARM");
                str.AppendLine("TRIGGER:-PT15M");
                str.AppendLine("ACTION:DISPLAY");
                str.AppendLine("DESCRIPTION:Reminder");
                str.AppendLine("END:VALARM");
                str.AppendLine("END:VEVENT");
                str.AppendLine("END:VCALENDAR");
                byte[] byteArray = Encoding.ASCII.GetBytes(str.ToString());
                MemoryStream stream = new MemoryStream(byteArray);
                await msg.AddAttachmentAsync("invite.ics", stream);
                //End: Attachment
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> CandidateCompleteTestLink(Mail model, string CandidateEmail, string ApplyingCompany, int score)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                var ccMails = model.CC.Split(";");
                foreach (var mail in ccMails)
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }

                object[] parameters = new object[] { CandidateEmail, ApplyingCompany, score };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "CandidateCompleteTest.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public async Task<string> SendQCPassNotificationToClient(List<string> clients, string candidateEmail, string primarySkill, string interviewStatus, string interviewDate)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Interview Notification");
                msg.Subject = "Candidate selected!";

                foreach (var email in clients)
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(email, email.Split("@").First()));
                }

                object[] parameters = new object[] { interviewDate, candidateEmail, primarySkill, interviewStatus };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "QCPassedNotificationTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed.";
            }
        }

        public async Task<string> SendB2BCandidateInvite(string ClientName, string jdLink, List<TransClientUser> clientUsers, string primarySkill, string calendlyLink, string emailId, string candidateName, string clientHREmail, string clientLogo = null, string clientURL = null, string clientvideoURL = null, string staffingAgency = null, int? irfId = null)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Interview Invite");
                msg.Subject = ClientName + " - Video Interview - " + primarySkill;
                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(emailId, "Candidate"));
                if (!string.IsNullOrEmpty(clientHREmail))
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(clientHREmail, clientHREmail.Split("@").First()));
                }
                if (!string.IsNullOrEmpty(staffingAgency))
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(staffingAgency, staffingAgency.Split("@").First()));
                }
                string companyLogoLink = string.Empty;
                string companyURL = string.Empty;
                companyLogoLink = clientLogo != null ? clientLogo : _configuration["RisebirdDetails:RisebirdLogo"];
                companyURL = clientURL != null ? clientURL : _configuration["RisebirdDetails:RisebirdURL"];
                object[] parameters = new object[] { companyURL, companyLogoLink, candidateName, ClientName, primarySkill, jdLink, calendlyLink, clientvideoURL };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InviteCandidateB2BTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception ex)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> RBPayNotification(Mail model, string emailid)
        {
            try
            {

                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Payment Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }

                object[] parameters = new object[] { emailid };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "RBFNNotification.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> RBFNQueryNotification(Mail model, string query)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                var ccMails = model.CC.Split(";");
                foreach (var mail in ccMails)
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(mail, null));

                }
                object[] parameters = new object[] { query };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "RBFNQueryNotification.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> ChannelRequestNotification(Mail model, string client, string Skill, string Requester)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "New Channel Alert");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }

                object[] parameters = new object[] { client, Skill, Requester };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "ChannelManagementNotification.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> RBFNPaymentConfirmationNotification(Mail model, string amount, StringBuilder sb)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Payment Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                var ccMails = model.CC.Split(";");
                foreach (var mail in ccMails)
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(mail, null));

                }

                //if (!string.IsNullOrEmpty(fileName))
                //{
                //    byte[] byteData = Encoding.ASCII.GetBytes(File.ReadAllText(fileName));

                //    var attachment = new SendGrid.Helpers.Mail.Attachment
                //    {
                //        Content = Convert.ToBase64String(byteData),
                //        Filename = fileName,
                //        Type = "txt/plain",
                //        Disposition = "attachment"
                //    };
                //    msg.AddAttachment(attachment);
                //}


                object[] parameters = new object[] { amount, sb.ToString() };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "RBFNPaymentConfirmationNotification.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception ex)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        //private SendGridMessage ConstructMailMessageObject(string to, string subject, string from = null, string ccEmail = null, List<string> ccList = null, List<string> bccList = null)
        //{
        //    var msg = new SendGridMessage();
        //    if (from == null)
        //    {
        //        msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");

        //    }
        //    else
        //    {
        //        msg.From = new SendGrid.Helpers.Mail.EmailAddress(from, from.Split("@").First());

        //    }
        //    msg.Subject = subject;
        //    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(to, to.Split("@").First()));

        //    if (ccEmail != null)
        //    {
        //        msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(ccEmail, ccEmail.Split("@").First()));

        //    }

        //    if (ccList != null && ccList.Count() != 0)
        //    {
        //        foreach (var email in ccList)
        //        {
        //            msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(email, email.Split("@").First()));

        //        }

        //    }
        //    if (bccList != null && bccList.Count() != 0)
        //    {
        //        foreach (var email in bccList)
        //        {
        //            msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(email, email.Split("@").First()));

        //        }
        //    }
        //    return msg;
        //}
        private async Task<string> SendMail(SendGridMessage msg, int? IRFId = null, int? interviewId = null)
        {
            try
            {
                var currentUser = _httpContextAccessor.HttpContext.Session.GetString("email")?.Trim();

                var client = new SendGridClient(_sendGridClientKey);

                var response = await client.SendEmailAsync(msg);
                if (response.StatusCode != HttpStatusCode.Accepted)
                {
                    return "Failed to send mail. Please check all fields and try again.";

                }
                SentMailDetails obj = new SentMailDetails();
                IEnumerable<string> values;
                //string message_id; 
                if (response.Headers.TryGetValues("X-Message-Id", out values))
                {
                    obj.sg_message_id = values?.FirstOrDefault();
                }

                obj.MailSubject = msg.Subject;
                StringBuilder tos = new StringBuilder();
                StringBuilder ccs = new StringBuilder();
                StringBuilder bccs = new StringBuilder();

                if (msg.Personalizations != null)//foreach (var item in msg.Personalizations)
                {
                    var item = msg.Personalizations[0];
                    if (tos.Length == 0)
                    {
                        if (item.Tos != null)
                        {
                            tos.Append(string.Join(",", item.Tos.Select(x => x.Email)));
                        }
                    }
                    else
                    {
                        tos = tos.Append(",");
                        tos.Append(string.Join(",", item.Tos.Select(x => x.Email)));
                    }
                    if (ccs.Length == 0)
                    {
                        if (item.Ccs != null)
                        {
                            ccs.Append(string.Join(",", item.Ccs.Select(x => x.Email)));
                        }
                    }
                    else
                    {
                        ccs = ccs.Append(",");
                        ccs.Append(string.Join(",", item.Ccs.Select(x => x.Email)));
                    }
                    if (bccs.Length == 0)
                    {
                        if (item.Bccs != null)
                        {
                            bccs.Append(string.Join(",", item.Bccs.Select(x => x.Email)));
                        }
                    }
                    else
                    {
                        bccs = bccs.Append(",");
                        bccs.Append(string.Join(",", item.Bccs.Select(x => x.Email)));
                    }
                }
                obj.To = tos != null ? tos.ToString() : null;
                obj.Cc = ccs != null ? ccs.ToString() : null;
                obj.Bcc = bccs != null ? bccs.ToString() : null;
                obj.MailBody = msg.Contents?[0].Value;
                obj.IRFId = IRFId;
                obj.InterviewId = interviewId;
                obj.SentBy = currentUser;
                _ = await _sendGridRepository.SaveSentMailDetails(obj);
                return "Mail sent successfully.";
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public async Task<string> SendCandidateLastStatusMail(List<string> clientEmails, List<string> candidates, string irfName, string primarySkill)
        {
            try
            {

                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = "Candidate status for yesterday (" + DateTime.Now.AddDays(-1).ToString("dd MMMM yyyy") + ")";

                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird"));
                foreach (var clientEmail in clientEmails)
                {
                    if (!string.IsNullOrEmpty(clientEmail))
                    {
                        msg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(clientEmail, null));
                    }
                }

                DataTable dt = new DataTable();
                dt.Columns.AddRange(new DataColumn[3] { new DataColumn("Name", typeof(string)),
                    new DataColumn("Email", typeof(string)),
                    new DataColumn("Status",typeof(string))   });
                foreach (var candidate in candidates)
                {
                    dt.Rows.Add(candidate.Split('|')[0], candidate.Split('|')[1], candidate.Split('|')[2]);
                }

                StringBuilder sb = new StringBuilder();
                //Table start.
                sb.Append("<table cellpadding='5' cellspacing='0' style='border: 1px solid #ccc;font-size: 9pt;font-family:Arial'>");

                //Adding HeaderRow.
                sb.Append("<tr>");
                foreach (DataColumn column in dt.Columns)
                {
                    sb.Append("<th style='background-color: #B8DBFD;border: 1px solid #ccc; width:200px'>" + column.ColumnName + "</th>");
                }
                sb.Append("</tr>");


                //Adding DataRow.
                foreach (DataRow row in dt.Rows)
                {
                    sb.Append("<tr>");
                    foreach (DataColumn column in dt.Columns)
                    {
                        sb.Append("<td style='width:100px;border: 1px solid #ccc'>" + row[column.ColumnName].ToString() + "</td>");
                    }
                    sb.Append("</tr>");
                }

                //Table end.
                sb.Append("</table>");


                object[] parameters = new object[] { irfName, primarySkill, sb };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "CandidateLastStatusTemplate.html");

                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception ex)
            {
                 await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;

            }
        }
        public async Task<string> CandidateShortlistNotification(Mail model, string currenturl, string comment, string email, string[] videoidlist)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Payment Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                StringBuilder sr = new StringBuilder();
                int i = 0;
                foreach (var item in videoidlist)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        i++;
                        sr.Append(i);
                        sr.Append(") ");
                        sr.Append(currenturl + item);
                        sr.Append("\n");
                    }
                }
                object[] parameters = new object[] { email, sr.ToString(), comment };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewershortlistNotification.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception ex)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> InterviewerWaitingNotification(Mail model, string zoomlink, string whowaiting, string candidateName, string InterviewDate)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Interview Alert");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                var ccMails = model.CC.Split(";");
                foreach (var mail in ccMails)
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                if (whowaiting == "Interviewer")
                {
                    object[] parameters = new object[] { zoomlink, candidateName, InterviewDate };
                    string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewerWaitingNotification.html");
                    string msgBody = string.Format(rawEmail, parameters);
                    msg.HtmlContent = msgBody;
                    return SendMail(msg).Result;
                }
                else
                {
                    object[] parameters = new object[] { zoomlink };
                    string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "CandidateWaitingNotification.html");
                    string msgBody = string.Format(rawEmail, parameters);
                    msg.HtmlContent = msgBody;
                    return await SendMail(msg);
                }
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> CandidateWaitingEINotification(Mail model, string zoomlink)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Interview Alert");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                msg.AddTo(msg.From);
                foreach (var mail in toMails)
                {
                    msg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                var ccMails = model.CC.Split(";");
                foreach (var mail in ccMails)
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                object[] parameters = new object[] { zoomlink };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "EmergencyInterviewerNotification.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> AppologyNotificationToCandidate(Mail model, string candidateName)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Interview Alert");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");

                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                var ccMails = model.CC.Split(";");
                foreach (var mail in ccMails)
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                object[] parameters = new object[] { candidateName };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "CandidateWaitingForInterviewer.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> ReScheduleNotificationToCandidate(Mail model, string zoomlink, string candidateName)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Interview Alert");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                var ccMails = model.CC.Split(";");
                foreach (var mail in ccMails)
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                object[] parameters = new object[] { zoomlink, candidateName };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "RescheduleInterview.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> AppologyNotificationToCandidateSecond(Mail model, string candidateName)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Interview Alert");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");

                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                var ccMails = model.CC.Split(";");
                foreach (var mail in ccMails)
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                object[] parameters = new object[] { candidateName };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "CandidateWaitingApologyForInterviewer.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> InstaClientNotification(Mail model, string RequiredSkill)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                object[] parameters = new object[] { RequiredSkill };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InstaInterviewRequest.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> InstaInterviewerNotification(Mail model, string RequiredSkill)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mailFrom, null));
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                object[] parameters = new object[] { RequiredSkill };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InstaInterviewerRequestNotification.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public async Task<string> InstaReportClientNotification(Mail model, string UrlDetails, string CandidateName)
        {
            try
            {

                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }

                object[] parameters = new object[] { UrlDetails, CandidateName };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InstaReportClientNotification.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> InstaClientInterviewwithzoomlink(Mail model, string CandidateName, string zoomlink, DateTime Interviewdate)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                object[] parameters = new object[] { CandidateName, Interviewdate, zoomlink };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InstaClientInterviewLinkNotification.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> InstaInterviewwithzoomlink(Mail model, string CandidateName, string zoomlink, DateTime Interviewdate)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                object[] parameters = new object[] { CandidateName, Interviewdate, zoomlink };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InstaInterviewLinkNotification.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendPublicUrlToClient(Mail model, string UrlDetails)
        {
            try
            {

                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(model.CC, null));


                object[] parameters = new object[] { UrlDetails };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "PublicURLsendtoClient.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> InterviewRequestAddNotification(Mail model, string JdTitle, string PrimarySkill, string Client)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                object[] parameters = new object[] { Client, JdTitle, PrimarySkill };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewRequestAddNotification.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> InterviewRequestEditNotification(Mail model, string JdTitle, string PrimarySkill, string RequestedBy)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                object[] parameters = new object[] { JdTitle, PrimarySkill, RequestedBy };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewRequestEditNotification.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> InterviewRequestAssignationNotification(Mail model, string JdTitle, string PrimarySkill, string interviewer)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                object[] parameters = new object[] { JdTitle, PrimarySkill, interviewer };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewRequestAssignationNotification.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> InterviewRequestCompletedNotification(Mail model, string JdTitle, string PrimarySkill, string Country, string JobRole, string CalendlyLink, string IRFID)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                object[] parameters = new object[] { JdTitle, PrimarySkill, Country, JobRole, CalendlyLink, IRFID };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewRequestCompletedNotification.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> InterviewRequestAddClientNotification(Mail model, string JdTitle, string PrimarySkill)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                object[] parameters = new object[] { JdTitle, PrimarySkill };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewRequestAddClientNotification.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendInterviewCancelledMailClient(string interviewDate, string CandidateName, List<string> irfOwnersAndRestrictedIrfOwners = null, int? irfId = null, int? interviewId = null)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Interview Cancelled");
                msg.Subject = "Interview has been Cancelled for the date: " + interviewDate;

                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird"));
                if (irfOwnersAndRestrictedIrfOwners != null && irfOwnersAndRestrictedIrfOwners.Count != 0)
                {
                    foreach (var clientEmail in irfOwnersAndRestrictedIrfOwners)
                    {
                        msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(clientEmail, null));
                    }
                }
                object[] parameters = new object[] { CandidateName, interviewDate };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewCancelledClientNotification.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;
                return await SendMail(msg, irfId, interviewId);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendInterviewCancelledMailCandidate(string CandidateEmail, string interviewDate, string CandidateName, int? irfId, int? interviewId)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Interview Cancel");
                msg.Subject = "Interview has been Cancelled for the date: " + interviewDate;

                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(CandidateEmail, null));
                StringBuilder msgBody = new StringBuilder("Dear ," + CandidateName);
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("You have received this communication as your interview has been cancelled for interview date: " + interviewDate + " due to some unforeseen circumstances.");
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("Thank You, Team Risebird");

                msg.HtmlContent = msgBody.ToString();

                return await SendMail(msg, irfId, interviewId);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendPublicUrlToInterviewer(Mail model, string UrlDetails)
        {
            try
            {

                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(model.CC, null));


                object[] parameters = new object[] { UrlDetails };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "PublicURLsendtoInterviewer.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendInterviewerCommonInterviewNotification(Mail model, string UrlDetails, int totalCount)
        {
            try
            {

                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(model.CC, null));


                object[] parameters = new object[] { totalCount, UrlDetails };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewerCommonInterviewNotification.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendPasswordresetNotification(Mail model)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification"));
                foreach (var mail in toMails)
                {
                    msg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(model.CC, null));
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "PasswordresetNotification.html");
                string msgBody = string.Format(rawEmail, null);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendInterviewInvite(Mail model, string Email, string join_url, string StaffingAgiencies, string HRCoordinator, string accountemail, DateTime interviewdatetime, string Name, string timezonevalue, string ClientName, int offset)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Interview Alert");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");

                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                var ccMails = model.CC.Split(";");
                foreach (var mail in ccMails)
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                if (!string.IsNullOrWhiteSpace(HRCoordinator))
                {
                    var hrEmail = HRCoordinator.Split("@");
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(HRCoordinator, hrEmail.FirstOrDefault()));
                }
                if (!string.IsNullOrWhiteSpace(StaffingAgiencies))
                {
                    var staffingEmail = StaffingAgiencies.Split("@");
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(StaffingAgiencies, staffingEmail.FirstOrDefault()));
                }
                var interviewdate = interviewdatetime.ToString() + "(" + timezonevalue + ")";
                object[] parameters = new object[] { interviewdate, join_url, Email, accountemail, HRCoordinator, StaffingAgiencies, Name, ClientName };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "schedulingmeeting.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                //Start: Attachment
                StringBuilder str = new StringBuilder();
                str.AppendLine("BEGIN:VCALENDAR");
                str.AppendLine("PRODID:-//Schedule a Meeting");
                str.AppendLine("VERSION:2.0");
                str.AppendLine("METHOD:REQUEST");
                str.AppendLine("BEGIN:VEVENT");
                str.AppendLine(string.Format("DTSTART:{0:yyyyMMddTHHmmssZ}", interviewdatetime.AddMinutes(offset)));
                str.AppendLine(string.Format("DTSTAMP:{0:yyyyMMddTHHmmssZ}", DateTime.UtcNow));
                str.AppendLine(string.Format("DTEND:{0:yyyyMMddTHHmmssZ}", interviewdatetime.AddMinutes(offset).AddMinutes(60)));
                str.AppendLine("LOCATION:" + join_url);
                str.AppendLine(string.Format("UID:{0}", Guid.NewGuid()));
                str.AppendLine(string.Format("SUMMARY:{0}", msg.Subject));
                str.AppendLine(string.Format("ATTENDEE;CN=\"{0}\";RSVP=TRUE:mailto:{1}", toMails[0], toMails[0]));
                str.AppendLine("BEGIN:VALARM");
                str.AppendLine("TRIGGER:-PT15M");
                str.AppendLine("ACTION:DISPLAY");
                str.AppendLine("DESCRIPTION:Reminder");
                str.AppendLine("END:VALARM");
                str.AppendLine("END:VEVENT");
                str.AppendLine("END:VCALENDAR");
                byte[] byteArray = Encoding.ASCII.GetBytes(str.ToString());
                MemoryStream stream = new MemoryStream(byteArray);
                await msg.AddAttachmentAsync("invite.ics", stream);
                //End: Attachment
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendInterviewCancelledMailCandidateWithSchedulingLink(string CandidateEmail, string interviewDate, string CandidateName, string rescheduingLink, int? irfId = null, int? interviewId = null)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Interview Cancel");
                msg.Subject = "Interview has been Cancelled for the date: " + interviewDate;

                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(CandidateEmail, null));
                StringBuilder msgBody = new StringBuilder("Dear ," + CandidateName);
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("You have received this communication as your interview has been cancelled for interview date: " + interviewDate + " due to unforeseen circumstances. Kindly reschedule your interview using below rescheduling link.");
                msgBody.Append("\n");
                msgBody.Append("<a href=\"" + rescheduingLink + "\">Rescheuling Link</a>");
                msgBody.Append("\n");
                msgBody.AppendLine("Thank You, Team Risebird");

                msg.HtmlContent = msgBody.ToString();

                return await SendMail(msg, irfId, interviewId);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendInterviewReminder(Mail model, string CandidateName, DateTime interviewdate, string ClientName, string PrimarySkillInvite, string yesbutton, string nobutton, string rescheulebutton, string needcleritybutton, string timezone, int? irfId = null, int? interviewId = null)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));

                }
                object[] parameters = new object[] { CandidateName, interviewdate.ToString(), yesbutton, nobutton, rescheulebutton, needcleritybutton, ClientName, PrimarySkillInvite, timezone };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewReminder.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg, irfId, interviewId);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendInterviewRemindersecond(Mail model, string CandidateName, DateTime interviewdate, string ClientName, string PrimarySkillInvite, string yesbutton, string nobutton, string rescheulebutton, string needcleritybutton, string timezone, int? irfId = null, int? interviewId = null)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));

                }
                object[] parameters = new object[] { CandidateName, interviewdate.ToString(), yesbutton, nobutton, rescheulebutton, needcleritybutton, ClientName, PrimarySkillInvite, timezone };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewReminder2.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg, irfId, interviewId);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendCheckIntervieweravailbility(Mail model, string Interviewname, string yesbutton, string nobutton, string Skillslist, string IRFName)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));

                }
                object[] parameters = new object[] { Interviewname, IRFName, yesbutton, nobutton, Skillslist };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "CheckIntervieweravailbility.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendAllIntervieweravailbility(Mail model, string Interviewname, string yesbutton, string nobutton, string cleritybutton, string nodropbutton)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));

                }
                object[] parameters = new object[] { Interviewname, yesbutton, nobutton, cleritybutton, nodropbutton };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "CheckAllIntervieweravailbility.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendClientCommonInterviewNotification(Mail model, DataTable dt)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(model.CC, null));

                StringBuilder sb = new StringBuilder();
                sb.Append("<table cellpadding='5' cellspacing='0' style='border: 1px solid #ccc;font-size: 9pt;font-family:Arial'>");
                sb.Append("<tr>");
                foreach (DataColumn column in dt.Columns)
                {
                    sb.Append("<th style='background-color: #B8DBFD;border: 1px solid #ccc; width:200px'>" + column.ColumnName + "</th>");
                }
                sb.Append("</tr>");
                foreach (DataRow row in dt.Rows)
                {
                    sb.Append("<tr>");
                    foreach (DataColumn column in dt.Columns)
                    {
                        sb.Append("<td style='width:100px;border: 1px solid #ccc'>" + row[column.ColumnName].ToString() + "</td>");
                    }
                    sb.Append("</tr>");
                }
                sb.Append("</table>");

                object[] parameters = new object[] { sb };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "ClientCommonInterviewNotification.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SharedTestLinkClient(Mail model, string title, string timeduration, string url, string CandidateEmail, string meetingurl, DateTime interviewdatetime, string Reporturl, int offset)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }

                object[] parameters = new object[] { title, timeduration, url, CandidateEmail, meetingurl, Reporturl };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "ClientInvitationTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                //Start: Attachment
                StringBuilder str = new StringBuilder();
                str.AppendLine("BEGIN:VCALENDAR");
                str.AppendLine("PRODID:-//Schedule a Meeting");
                str.AppendLine("VERSION:2.0");
                str.AppendLine("METHOD:REQUEST");
                str.AppendLine("BEGIN:VEVENT");
                str.AppendLine(string.Format("DTSTART:{0:yyyyMMddTHHmmssZ}", interviewdatetime.AddMinutes(offset)));
                str.AppendLine(string.Format("DTSTAMP:{0:yyyyMMddTHHmmssZ}", DateTime.UtcNow));
                str.AppendLine(string.Format("DTEND:{0:yyyyMMddTHHmmssZ}", interviewdatetime.AddMinutes(offset + 60)));
                str.AppendLine("LOCATION:" + meetingurl);
                str.AppendLine(string.Format("UID:{0}", Guid.NewGuid()));
                str.AppendLine(string.Format("SUMMARY:{0}", msg.Subject));
                str.AppendLine(string.Format("ATTENDEE;CN=\"{0}\";RSVP=TRUE:mailto:{1}", toMails[0], toMails[0]));
                str.AppendLine("BEGIN:VALARM");
                str.AppendLine("TRIGGER:-PT15M");
                str.AppendLine("ACTION:DISPLAY");
                str.AppendLine("DESCRIPTION:Reminder");
                str.AppendLine("END:VALARM");
                str.AppendLine("END:VEVENT");
                str.AppendLine("END:VCALENDAR");
                byte[] byteArray = Encoding.ASCII.GetBytes(str.ToString());
                MemoryStream stream = new MemoryStream(byteArray);
                await msg.AddAttachmentAsync("invite.ics", stream);
                //End: Attachment
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendUnassignedInterviewsNotifications(List<string> interviwers, string meetingDate)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Urgent");
                msg.Subject = "Assign : Interview has been scheduled on date: " + meetingDate;

                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird"));
                foreach (var mails in interviwers)
                {
                    msg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(mails, null));
                }
                object[] parameters = new object[] { meetingDate };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "UnassignedInterviewNotification.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendResumeUploadCounts(Mail model, DataTable dt, string Name)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                var ccMails = model.CC.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                foreach (var mail in ccMails)
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("<table cellpadding='5' cellspacing='0' style='border: 1px solid #ccc;font-size: 9pt;font-family:Arial'>");
                sb.Append("<tr>");
                foreach (DataColumn column in dt.Columns)
                {
                    sb.Append("<th style='background-color: #B8DBFD;border: 1px solid #ccc; width:200px'>" + column.ColumnName + "</th>");
                }
                sb.Append("</tr>");
                foreach (DataRow row in dt.Rows)
                {
                    sb.Append("<tr>");
                    foreach (DataColumn column in dt.Columns)
                    {
                        sb.Append("<td style='width:100px;border: 1px solid #ccc'>" + row[column.ColumnName].ToString() + "</td>");
                    }
                    sb.Append("</tr>");
                }
                sb.Append("</table>");

                object[] parameters = new object[] { Name, sb };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "ResumeDetailsCountNotification.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendQCFormDetails(Mail model, DataTable dt)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                var ccMails = model.CC.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                foreach (var mail in ccMails)
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("<table cellpadding='5' cellspacing='0' style='border: 1px solid #ccc;font-size: 9pt;font-family:Arial'>");
                sb.Append("<tr>");
                foreach (DataColumn column in dt.Columns)
                {
                    sb.Append("<th style='background-color: #B8DBFD;border: 1px solid #ccc; width:200px'>" + column.ColumnName + "</th>");
                }
                sb.Append("</tr>");
                foreach (DataRow row in dt.Rows)
                {
                    sb.Append("<tr>");
                    foreach (DataColumn column in dt.Columns)
                    {
                        sb.Append("<td style='width:100px;border: 1px solid #ccc'>" + row[column.ColumnName].ToString() + "</td>");
                    }
                    sb.Append("</tr>");
                }
                sb.Append("</table>");

                object[] parameters = new object[] { sb };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "QCFormDetailsNotification.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendSkillChannelStatsDetails(Mail model, string Name, DataTable dt)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(model.CC, null));

                StringBuilder sb = new StringBuilder();
                sb.Append("<table cellpadding='5' cellspacing='0' style='border: 1px solid #ccc;font-size: 9pt;font-family:Arial'>");
                sb.Append("<tr>");
                foreach (DataColumn column in dt.Columns)
                {
                    sb.Append("<th style='background-color: #B8DBFD;border: 1px solid #ccc; width:200px'>" + column.ColumnName + "</th>");
                }
                sb.Append("</tr>");
                foreach (DataRow row in dt.Rows)
                {
                    sb.Append("<tr>");
                    foreach (DataColumn column in dt.Columns)
                    {
                        sb.Append("<td style='width:100px;border: 1px solid #ccc'>" + row[column.ColumnName].ToString() + "</td>");
                    }
                    sb.Append("</tr>");
                }
                sb.Append("</table>");

                object[] parameters = new object[] { Name, sb };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "ClientSkillChannelStatsNotification.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendInterviewAadhaarVerificationNotification(Mail model, string UrlDetails, string Name)
        {
            try
            {

                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                //msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(model.CC, null));


                object[] parameters = new object[] { Name, UrlDetails };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "AadhaarVerificationNotification.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendInteviewerRemovalNotification(Mail model, int totalinterview, int selectedinterview, int rejectedinterview, string interviewer, string Client, string IRFId, string clientemail)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                var ccMails = model.CC.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                foreach (var mail in ccMails)
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                object[] parameters = new object[] { Client, IRFId, interviewer, totalinterview, selectedinterview, rejectedinterview, clientemail };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewerRemovalNotification.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendRedoCandidateInvite(string ClientName, string jdLink, List<TransClientUser> clientUsers, string primarySkill, string calendlyLink, string emailId, string candidateName, string clientHREmail, string staffingAgency = null)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Interview Invite");
                msg.Subject = ClientName + " - Video Interview - " + primarySkill;
                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(emailId, emailId.Split("@").First()));
                if (clientUsers != null && clientUsers.Count > 0)
                {
                    foreach (var clientUser in clientUsers)
                    {
                        msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(clientUser.EmailId, clientUser.EmailId.Split("@").First()));
                    }
                    if (!string.IsNullOrEmpty(staffingAgency) && !clientUsers.Any(x => x.EmailId == staffingAgency))
                    {
                        msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(staffingAgency, staffingAgency.Split("@").First()));
                    }
                }
                else if (!string.IsNullOrEmpty(clientHREmail))
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(clientHREmail, clientHREmail.Split("@").First()));
                }

                object[] parameters = new object[] { candidateName, ClientName, primarySkill, jdLink, calendlyLink };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "RedoInviteCandidate.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception ex)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendRedoClientInvite(string ClientName, List<TransClientUser> clientUsers, string primarySkill, string calendlyLink, string emailId, string candidateName, string clientHREmail, string interviewdate, string staffingAgency = null)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Interview Invite");
                msg.Subject = ClientName + " - Video Interview - " + primarySkill;
                if (clientUsers != null && clientUsers.Count > 0)
                {
                    foreach (var clientUser in clientUsers)
                    {
                        msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(clientUser.EmailId, clientUser.EmailId.Split("@").First()));
                    }
                    if (!string.IsNullOrEmpty(staffingAgency) && !clientUsers.Any(x => x.EmailId == staffingAgency))
                    {
                        msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(staffingAgency, staffingAgency.Split("@").First()));
                    }
                }
                else if (!string.IsNullOrEmpty(clientHREmail))
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(clientHREmail, clientHREmail.Split("@").First()));
                }

                object[] parameters = new object[] { ClientName, emailId, interviewdate, primarySkill };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "RedoInviteClient.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendClientInterviewPublicURL(Mail model, string Name, string publicurl)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(model.CC, null));

                object[] parameters = new object[] { Name, publicurl };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "ClientInterviewComplentMappedPublic.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SendInterviewClerityClientNotification(string CandidateEmail, string interviewDate, string ClientHR, string CandidateName, List<string> irfOwnersAndRestrictedIrfOwners = null)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = "Candidate : " + CandidateName + " : raise request for clerity about Interview";
                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird"));
                if (!string.IsNullOrEmpty(ClientHR))
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(ClientHR, null));
                }
                if (irfOwnersAndRestrictedIrfOwners != null && irfOwnersAndRestrictedIrfOwners.Count != 0)
                {
                    foreach (var clientEmail in irfOwnersAndRestrictedIrfOwners)
                    {
                        msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(clientEmail, null));
                    }
                }
                object[] parameters = new object[] { CandidateEmail, interviewDate };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewClerityClientNotification.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public async Task<string> SendInterviewNoReminder(Mail model, string CandidateName, DateTime interviewdate, string ClientName, string PrimarySkillInvite, string timezone, int? irfId = null, int? interviewId = null)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));

                }
                object[] parameters = new object[] { CandidateName, interviewdate.ToString(), ClientName, PrimarySkillInvite, timezone };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewNoReminder.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg, irfId, interviewId);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public async Task<string> SharedBotTestLink(Mail model, string title, string timeduration, string url, string ClientName)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;
                var toMails = model.TO.Split(";");
                foreach (var mail in toMails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }

                object[] parameters = new object[] { title, timeduration, url, ClientName };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "CandidateBotInvitationTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return await SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
    }
}
