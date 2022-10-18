using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface INotificationHelper
    {
        Task<string> SendHelpsupportMail(Mail model);
        Task<string> SendIRFBasedInterviewerMail(List<string> interviwers, string meetingDate);
        Task<string> SendInterviewerMailOnAssinment(string interviwers, string meetingDate);
        //string SendAlertHRForUnassignedInterview(List<string> hrs, string meetingDate, string candidateEmail, string interviewer, string Skillchannelid, string Client);
        //string SendIRFBasedClientMail(List<TransClientUser> clientUsers, string meetingDate, string jdTitle, string candidateName, string reportLink, string interviewStatus, string videoLink, string interviewerComments);
        Task<string> SendCandidateInvite(string ClientName, string jdLink, List<TransClientUser> clientUsers, string primarySkill, string calendlyLink, string emailId, string candidateName, string clientHREmail, string staffingAgency = null);
        Task<string> CancelCandidateInvite(string ClientName, List<TransClientUser> clientUsers, string primarySkill, string emailId);
        // Task<string> SendQCInterviewerMail(List<string> interviwers, string meetingDate);
        Task<string> SendInterviewCancelledMail(List<string> interviewers, string interviewDate, List<string> irfOwnersAndRestrictedIrfOwners = null);
        Task<string> SendCandidateOnboardedMail(List<string> mailIds, string candidateName, string irfName, string evaluator);
        Task<string> SendInterviewCancelledMail(string interviewer, string interviewDate, string IRFid);
        Task<string> SendInterviewCancelledMailtoCandidate(string candidateMail, string clientHRMail, string interviewDate);
        Task<string> CuretatedNotification(Mail model, string UserType, string EmailParameter);
        Task<string> SendCuratedUserMail(List<string> curatedUsers, string meetingDate, string irf, string primarySkill, string codedName);
        Task<string> SharedVideoNofitication(Mail model, string UrlDetails, string Skills);
        Task<string> SharedVideoNofiticationExternal(Mail model, string UrlDetails, string Url);
        Task<string> SharedVideoNofiticationExternal(Mail model, string name, string Url, string comment, string resumeLink);
        Task<string> SendBroadcastMail(List<string> receivers, List<string> supportEmails, BroadcastModel model);
        Task<string> SendCommonPoolMail(List<string> clientUsers, string clientEmailId, Candidate candidateDetails);

        Task<string> SendInviteToInterviewer(string iscName, string jdTitle, string primarySkill, string calendlyLink, string interviewerMailId, string hrEmailId);
        Task<string> CancelInterviewerInvite(string iscName, string primarySkill, string interviewerMailId, string hrEmailId);
        Task<string> MeetingInvite(DateTime startTime, DateTime endTime, string TimeZone, string accountMail, string accountPass, string Message, string subject, string sender, string candidateMail, string additionMail, string meetingURL);
        Task<string> VideoAccessRequest(Mail model, string url, string Emailid);
        Task<string> ShowIntrestInterview(Mail model, string Email, string videourl);
        Task<string> SendAssessmentNotification(Mail model, string emailid);
        Task<string> SendAssessmentNotificationComments(Mail model, string assessment, string comments);
        Task<string> SendVmStopReminder(VMActionPerformed obj);

        Task<string> CandidateJDScoreNotification(Mail model, int candidateCount, string JDName);
        Task<string> SurveyNotification(Mail model, string UrlDetails);
        Task<string> FeedbackSubmitNotification(Mail model, string candidate);
        Task<string> InsuficientBalancNotifiaction(Mail model);
        Task<string> SendQCPassNotificationToClient(List<string> clients, string candidateEmail, string primarySkill, string interviewStatus, string interviewDate);
        Task<string> SharedTestLink(Mail model, string title, string timeduration, string Url, string ClientName, string meetingurl, DateTime interviewdatetime, int offset);
        Task<string> CandidateCompleteTestLink(Mail model, string CandidateEmail, string ApplyingCompany, int score);
        string SendB2BCandidateInvite(string ClientName, string jdLink, List<TransClientUser> clientUsers, string primarySkill, string calendlyLink, string emailId, string candidateName, string clientHREmail, string clientLogo = null, string clientURL = null, string clientvideoURL = null, string staffingAgency = null);
        string RBPayNotification(Mail model, string emailid);
        string RBFNQueryNotification(Mail model, string query);
        string ChannelRequestNotification(Mail model, string client, string Skill, string Requester);
        string RBFNPaymentConfirmationNotification(Mail model, string amount, StringBuilder sb);
        string SendCandidateLastStatusMail(List<string> clientEmails, List<string> candidates, string irfName, string primarySkill);
        string CandidateShortlistNotification(Mail model, string currenturl, string comment, string email, string[] videoidlist);
        string InterviewerWaitingNotification(Mail model, string zoomlink, string whowaiting, string candidateName, string InterviewDate);
        string CandidateWaitingEINotification(Mail model, string zoomlink);
        string AppologyNotificationToCandidate(Mail model, string candidateName);
        string ReScheduleNotificationToCandidate(Mail model, string zoomlink, string candidateName);
        string AppologyNotificationToCandidateSecond(Mail model, string candidateName);
        string InstaClientNotification(Mail model, string RequiredSkill);
        string InstaInterviewerNotification(Mail model, string RequiredSkill);
        string InstaInterviewwithzoomlink(Mail model, string CandidateName, string zoomlink, DateTime Interviewdate);
        string InstaReportClientNotification(Mail model, string UrlDetails, string CandidateName);
        string InstaClientInterviewwithzoomlink(Mail model, string CandidateName, string zoomlink, DateTime Interviewdate);
        string SendPublicUrlToClient(Mail model, string UrlDetails);
        string InterviewRequestAddNotification(Mail model, string JdTitle, string PrimarySkill, string Client);
        string InterviewRequestEditNotification(Mail model, string JdTitle, string PrimarySkill, string RequestedBy);
        string InterviewRequestAssignationNotification(Mail model, string JdTitle, string PrimarySkill, string interviewer);
        string InterviewRequestCompletedNotification(Mail model, string JdTitle, string PrimarySkill, string Country, string JobRole, string CalendlyLink, string IRFID);
        string InterviewRequestAddClientNotification(Mail model, string JdTitle, string PrimarySkill);
        Task<string> SendInterviewCancelledMailClient(string interviewDate, string CandidateName, List<string> irfOwnersAndRestrictedIrfOwners = null);
        Task<string> SendInterviewCancelledMailCandidate(string CandidateEmail, string interviewDate, string CandidateName);
        string SendPublicUrlToInterviewer(Mail model, string UrlDetails);
        string SendInterviewerCommonInterviewNotification(Mail model, string UrlDetails, int totalCount);
        string SendPasswordresetNotification(Mail model);
        string SendInterviewInvite(Mail model, string Email, string join_url, string StaffingAgiencies, string HRCoordinator, string accountemail, DateTime interviewdatetime, string Name, string timezonevalue, string ClientName, int offset);
        Task<string> SendInterviewCancelledMailCandidateWithSchedulingLink(string CandidateEmail, string interviewDate, string CandidateName, string rescheduingLink);
        string SendInterviewReminder(Mail model, string CandidateName, DateTime interviewdate, string ClientName, string PrimarySkillInvite, string yesbutton, string nobutton, string rescheulebutton, string needcleritybutton, string timezone);
        string SendInterviewRemindersecond(Mail model, string CandidateName, DateTime interviewdate, string ClientName, string PrimarySkillInvite, string yesbutton, string nobutton, string rescheulebutton, string needcleritybutton, string timezone);
        string SendCheckIntervieweravailbility(Mail model, string Interviewname, string yesbutton, string nobutton, string Skillslist, string IRFName);
        string SendAllIntervieweravailbility(Mail model, string Interviewname, string yesbutton, string nobutton, string cleritybutton, string nodropbutton);
        string SendClientCommonInterviewNotification(Mail model, DataTable dt);
        Task<string> SharedTestLinkClient(Mail model, string title, string timeduration, string Url, string CandidateEmail, string meetingurl, DateTime interviewdatetime, string Reporturl, int offset);
        string SendUnassignedInterviewsNotifications(List<string> interviwers, string meetingDate);
        string SendResumeUploadCounts(Mail model, DataTable dt, string Name);
        string SendQCFormDetails(Mail model, DataTable dt);
        string SendSkillChannelStatsDetails(Mail model, string Name, DataTable dt);
        string SendInterviewAadhaarVerificationNotification(Mail model, string UrlDetails, string Name);
        string SendInteviewerRemovalNotification(Mail model, int totalinterview, int selectedinterview, int rejectedinterview, string interviewer, string Client, string IRFId, string clientemail);
        string SendRedoCandidateInvite(string ClientName, string jdLink, List<TransClientUser> clientUsers, string primarySkill, string calendlyLink, string emailId, string candidateName, string clientHREmail, string staffingAgency = null);
        string SendRedoClientInvite(string ClientName, List<TransClientUser> clientUsers, string primarySkill, string calendlyLink, string emailId, string candidateName, string clientHREmail, string interviewdate, string staffingAgency = null);
        string SendClientInterviewPublicURL(Mail model, string Name, string publicurl);
        string SendInterviewClerityClientNotification(string CandidateEmail, string interviewDate, string ClientHR, string CandidateName, List<string> irfOwnersAndRestrictedIrfOwners = null);
        string SendInterviewNoReminder(Mail model, string CandidateName, DateTime interviewdate, string ClientName, string PrimarySkillInvite, string timezone);

    }
}
