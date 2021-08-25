using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;

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
        private readonly string _sendGridClientKey = string.Empty;

        public NotificationHelper(IAzureStorageHelper azureStorageHelper, IConfiguration configuration, IApplicationErrorLogger errorLogger)
        {
            //_errorLogger = errorLogger;
            _configuration = configuration;
            _azureStorageHelper = azureStorageHelper;
            sendGridUserName = _configuration["SendGridKeySettings:UserName"];
            sendGridPassword = _configuration["SendGridKeySettings:Password"];
            mailFrom = _configuration["SendGridKeySettings:MailFrom"];
            _sendGridClientKey = _configuration["SendGridKeySettings:ApiKey"];
            _errorLogger = errorLogger;
        }

        public string SendHelpsupportMail(Mail model)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
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
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }


        public string SendIRFBasedInterviewerMail(List<string> interviwers, string meetingDate)
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
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewMailTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public string SendQCInterviewerMail(List<string> interviwers, string meetingDate)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird QC Alert");
                msg.Subject = "Interview dated: " + meetingDate + " submitted for quality check.";

                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird"));
                foreach (var mails in interviwers)
                {
                    msg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(mails, null));
                }

                object[] parameters = new object[] { meetingDate };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "QCInterviewMailTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public string SendAlertHRForUnassignedInterview(List<string> hrs, string meetingDate, string candidateEmail, string interviewer, string Skillchannelid, string Client)
        {
            try
            {

                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Unassigned Interview");
                msg.Subject = "HR Alert!!! " + interviewer + " Interviewer has unassigned interview for candidate " + candidateEmail + " scheduled on " + meetingDate;

                foreach (var mail in hrs)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                }
                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress("prasad_srinivas@risebird.io", "Prasad Srinivas"));
                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress("shweta_kadechkar@risebird.io", "Shweta Kadechkar"));
                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress("soumyashree_gudi@risebird.io", "Soumyashree Gudi"));

                StringBuilder msgBody = new StringBuilder("Dear HR,");
                msgBody.Append("\n");
                msgBody.AppendLine(
                    "HR Alert!!! " + interviewer + " Interviewer has unassigned interview for candidate " + candidateEmail + " scheduled on " + meetingDate + " for Client: " + Client + "(" + Skillchannelid + ").");
                msgBody.Append("\n");
                msgBody.AppendLine(
                    "Kindly login over risebird portal and click on Interview menu and assign interviewer for unassigned interview.");
                msgBody.Append("\n");
                msgBody.AppendLine("Note: Discuss interviewer if he/she has not informed HR before unassigning interview.");
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("Thank You, Team Risebird");

                msg.HtmlContent = msgBody.ToString();

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string SendInterviewerMailOnAssinment(string interviwers, string meetingDate)
        {
            try
            {

                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Interview Notification");
                msg.Subject = "New Interview has been assigned on date: " + meetingDate;

                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(interviwers, null));

                object[] parameters = new object[] { meetingDate };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InterviewAssingmentMailTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);


                msg.HtmlContent = msgBody;

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public string SendCandidateInvite(string ClientName, string jdLink, List<TransClientUser> clientUsers, string primarySkill, string calendlyLink, string emailId, string candidateName, string clientHREmail)
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
                }
                else if (!string.IsNullOrEmpty(clientHREmail))
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(clientHREmail, clientHREmail.Split("@").First()));
                }

                object[] parameters = new object[] { candidateName, ClientName, primarySkill, jdLink };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InviteCandidateTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        //CancelInviteCandidateTemplate
        public string CancelCandidateInvite(string ClientName, List<TransClientUser> clientUsers, string primarySkill, string emailId)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public string SendInterviewCancelledMail(List<string> interviewers, string interviewDate)
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
                msgBody.AppendLine("You have received this communication as an interview has been cancelled on interviewdate: " + interviewDate + " .");
                msgBody.AppendLine(
                    "Thanks for your support, we will keep updating you about future interview based on your matching skill.");
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("Thank You, Team Risebird");

                msg.HtmlContent = msgBody.ToString();

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public string SendCandidateOnboardedMail(List<string> mailIds, string candidateName, string irfName, string evaluator)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public string SendInterviewCancelledMail(string interviewer, string interviewDate, string IRFid)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public string SendInterviewCancelledMailtoCandidate(string candidateMail, string clientHRMail, string interviewDate)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string CuretatedNotification(Mail model, string UserType, string EmailParameter)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public string SendCuratedUserMail(List<string> curatedUsers, string meetingDate, string irf, string primarySkill, string codedName)
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
                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public string SharedVideoNofitication(Mail model, string UrlDetails, string skills)
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
                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string SharedVideoNofiticationExternal(Mail model, string UrlDetails, string url)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string SharedVideoNofiticationExternal(Mail model, string name, string url, string comment, string resumeLink)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string SendBroadcastMail(List<string> receivers, List<string> supportEmails, BroadcastModel model)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                msg.Subject = model.Subject;

                foreach (var supportEmail in supportEmails)
                {
                    msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(supportEmail, null));

                }
                foreach (var email in receivers)
                {
                    msg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(email, null));

                }

                object[] parameters = new object[] { model.BroadcastReciever, model.Message };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "BroadcastTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;


                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Broadcast failed.";
            }
        }
        public string SendInviteToInterviewer(string iscName, string jdTitle, string primarySkill, string calendlyLink, string interviewerMailId, string hrEmailId)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public string CancelInterviewerInvite(string iscName, string primarySkill, string interviewerMailId, string hrEmailId)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public string SendCommonPoolMail(List<string> clientUsers, string clientEmailId, Candidate candidateDetails)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed";
            }
        }
        public string MeetingInvite(DateTime startTime, DateTime endTime, string TimeZone, string accountMail, string accountPass, string Message, string subject, string sender, string candidateMail, string additionalMail, string meetingURL)
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

                return SendMail(msg);

            }
            catch (Exception)
            {
                return "Failed! Could not send the Email.";
            }
        }
        public string VideoAccessRequest(Mail model, string url, string Emailid)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string ShowIntrestInterview(Mail model, string Email, string videoURL)
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


                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string SendAssessmentNotification(Mail model, string emailid)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string SendAssessmentNotificationComments(Mail model, string assessment, string comments)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public string SendVmStopReminder(VMActionPerformed obj)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public string CandidateJDScoreNotification(Mail model, int candidateCount, string JDName)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string SurveyNotification(Mail model, string UrlDetails)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string FeedbackSubmitNotification(Mail model, string candidate)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string InsuficientBalancNotifiaction(Mail model)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string SharedTestLink(Mail model, string title, string timeduration, string url, string ClientName)
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
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "CandidateInvitationTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string CandidateCompleteTestLink(Mail model, string CandidateEmail, string ApplyingCompany, int score)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public string SendQCPassNotificationToClient(List<string> clients, string candidateEmail, string primarySkill, string interviewStatus, string interviewDate)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed.";
            }
        }

        public string SendB2BCandidateInvite(string ClientName, string jdLink, List<TransClientUser> clientUsers, string primarySkill, string calendlyLink, string emailId, string candidateName, string clientHREmail, string clientLogo = null, string clientURL = null, string clientvideoURL = null)
        {
            try
            {
                var msg = new SendGridMessage();
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Interview Invite");
                msg.Subject = ClientName + " - Video Interview - " + primarySkill;
                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(emailId, "Candidate"));

                //foreach (var clientUser in clientUsers)
                //{
                //    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(clientUser.EmailId, null));
                //}
                if (!string.IsNullOrEmpty(clientHREmail))
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(clientHREmail, clientHREmail.Split("@").First()));

                }


                string companyLogoLink = string.Empty;
                string companyURL = string.Empty;
                companyLogoLink = clientLogo != null ? clientLogo : _configuration["RisebirdDetails:RisebirdLogo"];
                companyURL = clientURL != null ? clientURL : _configuration["RisebirdDetails:RisebirdURL"];


                object[] parameters = new object[] { companyURL, companyLogoLink, candidateName, ClientName, primarySkill, jdLink, calendlyLink, clientvideoURL };
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InviteCandidateB2BTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string RBPayNotification(Mail model, string emailid)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string RBFNQueryNotification(Mail model, string query)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string ChannelRequestNotification(Mail model, string client, string Skill, string Requester)
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
                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string RBFNPaymentConfirmationNotification(Mail model, string amount, StringBuilder sb)
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

                //if (!String.IsNullOrEmpty(fileName))
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

                return SendMail(msg);
            }
            catch (Exception ex)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        private SendGridMessage ConstructMailMessageObject(string to, string subject, string from = null, string ccEmail = null, List<string> ccList = null, List<string> bccList = null)
        {
            var msg = new SendGridMessage();
            if (from == null)
            {
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");

            }
            else
            {
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(from, from.Split("@").First());

            }
            msg.Subject = subject;
            msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(to, to.Split("@").First()));

            if (ccEmail != null)
            {
                msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(ccEmail, ccEmail.Split("@").First()));

            }

            if (ccList != null && ccList.Count() != 0)
            {
                foreach (var email in ccList)
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(email, email.Split("@").First()));

                }

            }
            if (bccList != null && bccList.Count() != 0)
            {
                foreach (var email in bccList)
                {
                    msg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(email, email.Split("@").First()));

                }
            }
            return msg;
        }
        private string SendMail(SendGridMessage msg)
        {
            try
            {
                var client = new SendGridClient(_sendGridClientKey);

                var response = client.SendEmailAsync(msg).Result;
                if (response.StatusCode != HttpStatusCode.Accepted)
                {
                    return "Failed to send mail. Please check all fields and try again.";

                }
                return "Mail sent successfully.";
            }
            catch (Exception ex)
            {
                _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send mail. Please check all fields and try again.";
            }
        }

        public string SendCandidateLastStatusMail(List<string> clientEmails, List<string> candidates, string irfName, string primarySkill)
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
                return SendMail(msg);
            }
            catch (Exception ex)
            {
                _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;

            }
        }
        public string CandidateShortlistNotification(Mail model, string currenturl, string comment, string email, string[] videoidlist)
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
                    if (!String.IsNullOrEmpty(item))
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

                return SendMail(msg);
            }
            catch (Exception ex)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string InterviewerWaitingNotification(Mail model, string zoomlink, string whowaiting, string candidateName, string InterviewDate)
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
                    return SendMail(msg);
                }
                else
                {
                    object[] parameters = new object[] { zoomlink };
                    string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "CandidateWaitingNotification.html");
                    string msgBody = string.Format(rawEmail, parameters);
                    msg.HtmlContent = msgBody;
                    return SendMail(msg);
                }
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string CandidateWaitingEINotification(Mail model, string zoomlink)
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
                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string AppologyNotificationToCandidate(Mail model, string candidateName)
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
                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string ReScheduleNotificationToCandidate(Mail model, string zoomlink, string candidateName)
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
                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string AppologyNotificationToCandidateSecond(Mail model, string candidateName)
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
                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string InstaClientNotification(Mail model, string RequiredSkill)
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
                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string InstaInterviewerNotification(Mail model, string RequiredSkill)
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
                string rawEmail = _azureStorageHelper.GetBlobData("mailtemplate", "InstaInterviewerRequestNotification.html");
                string msgBody = string.Format(rawEmail, parameters);
                msg.HtmlContent = msgBody;
                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string InstaInterviewwithzoomlink(Mail model, string CandidateName, string zoomlink, DateTime Interviewdate)
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
                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
        public string InstaReportClientNotification(Mail model, string UrlDetails, string CandidateName)
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

                return SendMail(msg);
            }
            catch (Exception)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
    }
}
