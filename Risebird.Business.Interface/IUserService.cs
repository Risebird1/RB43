using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IUserService
    {
        List<Menu> GetMenuBasedOnUserEmailld(string emailId, string sessionId = null);
        List<UserList> GetRisebirdUsers();
        List<ClientUserVM> GetClientUsers(string EmailId);
        List<InterviewerVM> GetInterviewers();
        List<InterviewerVM> GetInterviewersDetails();

        List<QCInterviewerVM> GetQCInterviewers();
        IEnumerable<string> GetAllHrsEmailld();

        bool AddUser(UserList clientUser);
        bool DeleteUser(int id);
        UserList GetUser(int id);
        bool UpdateUser(UserList clientUser);

        bool AddClientUser(ClientUserVM clientUser);
        bool DeleteClientUser(int id);
        ClientUserVM GetClientUser(int id);
        bool UpdateClientUser(ClientUserVM clientUser);

        bool AddInterviewer(InterviewerVM interviewer);
        bool AddQCInterviewer(QCInterviewerVM qcinterviewer);
        InterviewerVM GetInterviewer(int id);
        List<InterviewerVM> GetInterviewerVideos(string Emailid);
        MeetingVM GetMeetingbyid(int id);

        bool DeleteInterviewer(int id);
        bool UpdateInterviewer(InterviewerVM interviewer);
        QCInterviewerVM GetQCInterviewer(int id);
        bool UpdateQCInterviewer(QCInterviewerVM qcInterviewer);
        bool DeleteQCInterviewer(int id);
        List<ResumeEvaluator> GetResumeEvaluators();
        bool AddResumeEvaluator(ResumeEvaluator resumeEvaluator);
        ResumeEvaluator GetResumeEvaluator(int id);
        bool EditResumeEvaluator(ResumeEvaluator resumeEvaluator);
        bool DeleteResumeEvaluator(int userId);
        List<Recruiter> GetRecruiters();
        bool AddRecruiter(Recruiter recruiter);
        bool EditRecruiter(Recruiter recruiter);
        Recruiter GetRecruiter(int id);
        bool DeleteRecruiter(int userId);
        void UpdateUserSigInDetails(string emailId);

        string GetCurrenttheme(string emailId);
        List<AssignmentVM> GetInterviewerEntitle();
        List<AssignmentVM> InterviewerEntitleDetails(int id);
        Task<bool> ResetNavigation(int userId);
        string GetCurrentuserImage(string emailId);
        string FetchWhatsAppNumberByEmail(string Emailid);
        string FetchNameByEmail(string Emailid);
        string SaveCallRecording(int interviewid, string IRFId, string CandidateEmail, string WhoCalled, string CallSid, string RecordingUrl);
        List<CallRecordingDetailsVM> FetchCallRecording(string candidateEmailid, string irfid);
        List<ZoomAccountDetailsVM> FetchInstaZoomAccounts();
        bool DeleteInstaZoomAccount(int Accountid);
        bool AddInstaZoomAccount(string AccountEmail);
        string GetCurrentUsername(string emailId);
        Task<bool> CheckMultiSession(string emailId);
        Task<bool> ClearSessionForOthersUsers(string emailId);
        AssignmentVM GetInterviewerEntitleById(int assessmentid);
        string SaveCallRecordingInterviewer(string CandidateEmail, string WhoCalled, string CallSid, string RecordingUrl);
        List<CallRecordingDetailsVM> FetchCallRecordingInterviewer(string Emailid);
        Task<string> SaveCallRecordingData(int? interviewId, string callRecipient, string caller, string CallSid, string RecordingSid, string callRecipientPhoneNumber = null, string IRFId = null);
    }
}
