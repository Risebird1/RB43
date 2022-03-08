using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IUserRepository
    {
        IEnumerable<Menu> GetMenuBasedOnUserEmailld(string emailId, string sessionId);
        IEnumerable<UserList> GetRisebirdUsers();
        IEnumerable<InterviewerVM> GetInterviewers();
        IEnumerable<InterviewerVM> GetInterviewersDetails();

        IEnumerable<ClientUserVM> GetClientUsers(string EmailId);
        Task<ClientUserVM> GetClientUserDetails(string emailID);
        IEnumerable<QCInterviewerVM> GetQCInterviewers();
        Task<bool> AddUser(UserList user);
        Task<bool> DeleteUser(int id);
        Task<UserList> GetUser(int id);
        Task<bool> UpdateUser(UserList user);

        Task<bool> AddClientUser(ClientUserVM client);
        Task<bool> DeleteClientUser(int id);
        Task<ClientUserVM> GetClientUser(int id);
        Task<bool> UpdateClientUser(ClientUserVM clientUser);

        Task<bool> AddInterviewer(InterviewerVM interviewer);
        Task<bool> AddQCInterviewer(QCInterviewerVM interviewer);
        Task<bool> DeleteInterviewer(int id);
        Task<InterviewerVM> GetInterviewer(int id);
        IEnumerable<InterviewerVM> GetInterviewerVideos(string Emailid);
        MeetingVM GetMeetingbyid(int id);

        Task<bool> UpdateInterviewer(InterviewerVM Interviewer);

        Task<QCInterviewerVM> GetQCInterviewer(int id);
        Task<bool> UpdateQCInterviewer(QCInterviewerVM qcInterviewer);
        IEnumerable<string> GetAllHrsEmailld();

        Task<bool> DeleteQCInterviewer(int id);
        IEnumerable<ResumeEvaluator> GetResumeEvaluators();
        Task<bool> AddResumeEvaluator(ResumeEvaluator resumeEvaluator);
        ResumeEvaluator GetResumeEvaluator(int id);
        bool EditResumeEvaluator(ResumeEvaluator resumeEvaluator);
        Task<bool> DeleteResumeEvaluator(int userId);
        IEnumerable<Recruiter> GetRecruiters();
        Task<bool> AddRecruiter(Recruiter recruiter);
        Task<bool> EditRecruiter(Recruiter recruiter);
        Recruiter GetRecruiter(int id);
        Task<bool> DeleteRecruiter(int userId);
        Task UpdateUserSigInDetails(string emailId);
        string GetCurrenttheme(string emailId);
        IEnumerable<AssignmentVM> GetInterviewerEntitle();
        IEnumerable<AssignmentVM> InterviewerEntitleDetails(int id);
        Task<bool> ResetNavigation(int userId);
        string GetCurrentuserImage(string emailId);

        string FetchWhatsAppNumberByEmail(string Emailid);
        string FetchNameByEmail(string Emailid);
        int GetInterviewerid(string emailid);
        string SaveCallRecording(int interviewid, string IRFId, string CandidateEmail, string WhoCalled, string CallSid, string RecordingUrl);
        List<CallRecordingDetailsVM> FetchCallRecording(string candidateEmailid, string irfid);
        List<string> GetUserClients(int id);
        List<ZoomAccountDetailsVM> FetchInstaZoomAccounts();
        bool DeleteInstaZoomAccount(int Accountid);
        bool AddInstaZoomAccount(string AccountEmail);
        string GetCurrentUsername(string emailId);
        bool CheckMultiSession(string emailId);
        bool ClearSessionForOthersUsers(string emailId);
        Task<bool> GetClientPasswordResetEnableStatus(int clientId);
        Task<int> DisableInactiveUser();
        string GetUserProfileBasedOnEmailId(string emailId);
    }
}
