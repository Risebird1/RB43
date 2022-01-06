using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.CommonHelper.Interface
{
    public interface INotificationHelper
    {
        string SendHelpsupportMail(Mail model);
        string SendIRFBasedInterviewerMail(List<string> interviwers, string meetingDate);
        string SendInterviewerMailOnAssinment(string interviwers, string meetingDate);
        string SendAlertHRForUnassignedInterview(List<string> hrs, string meetingDate, string candidateEmail, string interviewer, string Skillchannelid, string Client);
        string SendIRFBasedClientMail(List<TransClientUser> clientUsers, string meetingDate, string jdTitle, string candidateName, string reportLink, string interviewStatus, string videoLink, string interviewerComments);
        string SendCandidateInvite(string ClientName, string jdLink, List<TransClientUser> clientUsers, string primarySkill, string calendlyLink, string emailId, string candidateName, string clientHREmail, string staffingAgency = null);
        string CancelCandidateInvite(string ClientName, List<TransClientUser> clientUsers, string primarySkill, string emailId);
        string SendQCInterviewerMail(List<string> interviwers, string meetingDate);
        string SendInterviewCancelledMail(List<string> interviewers, string interviewDate, List<string> irfOwnersAndRestrictedIrfOwners = null);
        string SendCandidateOnboardedMail(List<string> mailIds, string candidateName, string irfName, string evaluator);
        string SendInterviewCancelledMail(string interviewer, string interviewDate, string IRFid);
        string SendInterviewCancelledMailtoCandidate(string candidateMail, string clientHRMail, string interviewDate);
        string CuretatedNotification(Mail model, string UserType, string EmailParameter);
        string SendCuratedUserMail(List<string> curatedUsers, string meetingDate, string irf, string primarySkill, string codedName);
        string SharedVideoNofitication(Mail model, string UrlDetails, string Skills);
        string SharedVideoNofiticationExternal(Mail model, string UrlDetails, string Url);
        string SharedVideoNofiticationExternal(Mail model, string name, string Url, string comment, string resumeLink);
        string SendBroadcastMail(List<string> receivers, List<string> supportEmails, BroadcastModel model);
        string SendCommonPoolMail(List<string> clientUsers, string clientEmailId, Candidate candidateDetails);

        string SendInviteToInterviewer(string iscName, string jdTitle, string primarySkill, string calendlyLink, string interviewerMailId, string hrEmailId);
        string CancelInterviewerInvite(string iscName, string primarySkill, string interviewerMailId, string hrEmailId);
        string MeetingInvite(DateTime startTime, DateTime endTime, string TimeZone, string accountMail, string accountPass, string Message, string subject, string sender, string candidateMail, string additionMail, string meetingURL);
        string VideoAccessRequest(Mail model, string url, string Emailid);
        string ShowIntrestInterview(Mail model, string Email, string videourl);
        string SendAssessmentNotification(Mail model, string emailid);
        string SendAssessmentNotificationComments(Mail model, string assessment, string comments);
        string SendVmStopReminder(VMActionPerformed obj);

        string CandidateJDScoreNotification(Mail model, int candidateCount, string JDName);
        string SurveyNotification(Mail model, string UrlDetails);
        string FeedbackSubmitNotification(Mail model, string candidate);
        string InsuficientBalancNotifiaction(Mail model);
        string SendQCPassNotificationToClient(List<string> clients, string candidateEmail, string primarySkill, string interviewStatus, string interviewDate);
        string SharedTestLink(Mail model, string title, string timeduration, string Url, string ClientName);
        string CandidateCompleteTestLink(Mail model, string CandidateEmail, string ApplyingCompany, int score);
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
        string SendInterviewCancelledMailClient(string interviewDate, string CandidateName, List<string> irfOwnersAndRestrictedIrfOwners = null);
        string SendInterviewCancelledMailCandidate(string CandidateEmail, string interviewDate, string CandidateName);
        string SendPublicUrlToInterviewer(Mail model, string UrlDetails);
        string SendInterviewerCommonInterviewNotification(Mail model, string UrlDetails, int totalCount);
        string SendPasswordresetNotification(Mail model);
        string SendInterviewInvite(Mail model, string Email, string join_url, string StaffingAgiencies, string HRCoordinator, string accountemail, DateTime interviewdatetime, string Name, string timezonevalue, string ClientName);
        string SendInterviewCancelledMailCandidateWithSchedulingLink(string CandidateEmail, string interviewDate, string CandidateName, string rescheduingLink);
        string SendInterviewReminder(Mail model, string CandidateName, DateTime interviewdate, string yesbutton, string nobutton);
        string SendInterviewRemindersecond(Mail model, string CandidateName, DateTime interviewdate, string yesbutton, string nobutton);
    }
}
