using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface INotificationHelper
    {
        string SendHelpsupportMail(Mail model);
        string SendIRFBasedInterviewerMail(List<string> interviwers, string meetingDate);
        string SendInterviewerMailOnAssinment(string interviwers, string meetingDate);
        string SendAlertHRForUnassignedInterview(List<string> hrs, string meetingDate, string candidateEmail, string interviewer);
        string SendIRFBasedClientMail(List<TransClientUser> clientUsers, string meetingDate, string jdTitle, string candidateName, string reportLink, string interviewStatus, string videoLink, string interviewerComments);
        string SendCandidateInvite(string ClientName, string jdLink, List<TransClientUser> clientUsers, string primarySkill, string calendlyLink, string emailId);
        string CancelCandidateInvite(string ClientName, List<TransClientUser> clientUsers, string primarySkill, string emailId);
        string SendQCInterviewerMail(List<string> interviwers, string meetingDate);
        string SendInterviewCancelledMail(List<string> interviewers, string interviewDate);
        string SendCandidateOnboardedMail(List<string> mailIds, string candidateName, string irfName, string evaluator);
        string SendInterviewCancelledMail(string interviewer, string interviewDate);
        string SendInterviewCancelledMailtoCandidate(string candidateMail, string clientHRMail, string interviewDate);
        string CuretatedNotification(Mail model, string UserType, string EmailParameter);
        string SendCuratedUserMail(List<string> curatedUsers, string meetingDate, string irf, string primarySkill, string codedName);
        string SharedVideoNofitication(Mail model,  string UrlDetails,string Skills);
        string SharedVideoNofiticationExternal(Mail model, string UrlDetails, string Url);
        string SharedVideoNofiticationExternal(Mail model, string name, string Url,string comment, string resumeLink);

    }
}
