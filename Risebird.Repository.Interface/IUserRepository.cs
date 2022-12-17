using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IUserRepository
    {
        Task<List<Menu>> GetMenuBasedOnUserEmailld(string emailId, string sessionId);
        Task<List<UserList>> GetRisebirdUsers();
        Task<List<InterviewerVM>> GetInterviewers();
        Task<List<InterviewerVM>> GetInterviewersDetails();

        Task<List<ClientUserVM>> GetClientUsers(string EmailId);
        Task<ClientUserVM> GetClientUserDetails(string emailID);
        Task<List<QCInterviewerVM>> GetQCInterviewers();
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
        Task<List<InterviewerVM>> GetInterviewerVideos(string Emailid);
        Task<MeetingVM> GetMeetingbyid(int id);

        Task<bool> UpdateInterviewer(InterviewerVM Interviewer);

        Task<QCInterviewerVM> GetQCInterviewer(int id);
        Task<bool> UpdateQCInterviewer(QCInterviewerVM qcInterviewer);
        Task<List<string>> GetAllHrsEmailld();

        Task<bool> DeleteQCInterviewer(int id);
        Task<List<ResumeEvaluator>> GetResumeEvaluators();
        Task<bool> AddResumeEvaluator(ResumeEvaluator resumeEvaluator);
        Task<ResumeEvaluator> GetResumeEvaluator(int id);
        Task<bool> EditResumeEvaluator(ResumeEvaluator resumeEvaluator);
        Task<bool> DeleteResumeEvaluator(int userId);
        Task<List<Recruiter>> GetRecruiters();
        Task<bool> AddRecruiter(Recruiter recruiter);
        Task<bool> EditRecruiter(Recruiter recruiter);
        Task<Recruiter> GetRecruiter(int id);
        Task<bool> DeleteRecruiter(int userId);
        Task UpdateUserSigInDetails(string emailId);
        Task<string> GetCurrenttheme(string emailId);
        Task<List<AssignmentVM>> GetInterviewerEntitle();
        Task<List<AssignmentVM>> InterviewerEntitleDetails(int id);
        Task<bool> ResetNavigation(int userId);
        Task<string> GetCurrentuserImage(string emailId);

        Task<string> FetchWhatsAppNumberByEmail(string Emailid);
        Task<string> FetchNameByEmail(string Emailid);
        Task<int> GetInterviewerid(string emailid);
        Task<string> SaveCallRecording(int interviewid, string IRFId, string CandidateEmail, string WhoCalled, string CallSid, string RecordingUrl);
        Task<List<CallRecordingDetailsVM>> FetchCallRecording(string candidateEmailid, string irfid);
        Task<List<string>> GetUserClients(int id);
        Task<List<ZoomAccountDetailsVM>> FetchInstaZoomAccounts();
        Task<bool> DeleteInstaZoomAccount(int Accountid);
        Task<bool> AddInstaZoomAccount(string AccountEmail);
         Task<string> GetCurrentUsername(string emailId);
        Task<bool> CheckMultiSession(string emailId);
         Task<bool> ClearSessionForOthersUsers(string emailId);
        Task<bool> GetClientPasswordResetEnableStatus(int clientId);
        Task<int> DisableInactiveUser();
        Task<string> GetUserProfileBasedOnEmailId(string emailId);
        Task<ClientUserVM> GetClientUserDetailsByIRF(string emailID, int irfid);
        Task<AssignmentVM> GetInterviewerEntitleById(int assessmentid);
        Task<string> SaveCallRecordingInterviewer(string CandidateEmail, string WhoCalled, string CallSid, string RecordingUrl);
        Task<List<CallRecordingDetailsVM>> FetchCallRecordingInterviewer(string Emailid);
        Task<string> SaveCallRecordingData(int? interviewId, string callRecipient, string caller, string callSid, string recordingSid, string callRecipientPhoneNumber = null, string IRFId = null);
        Task<List<CallRecordingDataVM>> GetRecordingDataToUpdate();
        Task<bool> UpdateRecordingData(CallRecordingDataVM obj);
        Task<bool> AddPremiumClientUser(ClientUserVM client);
        Task<List<ClientUserVM>> GetClientPremiumUsers(string EmailId);
        Task<ClientUserVM> GetClientPremiumUser(int id);
        Task<bool> UpdateClientPremiumUser(ClientUserVM clientUser);
        Task<ClientUserVM> GetClientPremiumUserDetails(string emailID);
        Task<string> GetWhatsappno(string emailId);
         Task<string> UpdateWhatsappno(string emailId, string WhatsappNumber);
        Task<ClientUserVM> GetClientDetailsByIRFId(string IRFId);
        Task<List<ClientUserVM>> GetClientUsersForReviewNotification();
    }
}
