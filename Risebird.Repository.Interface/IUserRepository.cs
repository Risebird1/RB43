using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IUserRepository
    {
        IEnumerable<Menu> GetMenuBasedOnUserEmailld(string emailId);
        IEnumerable<UserList> GetRisebirdUsers();
        IEnumerable<InterviewerVM> GetInterviewers();
        IEnumerable<ClientUserVM> GetClientUsers();
        ClientUserVM GetClientUserDetails(string emailID);
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

        Task<bool> AddOrEditInterviewerProfile(InterviewerProfileVM interviewerProfile);
        IEnumerable<string> GetNickNames(int UserId);
        IEnumerable<string> GetEmailIds(int UserId);

        Task<IEnumerable<InterviewerProfileVM>> GetInterviewerProfileById(int userid);
        Task<IEnumerable<TransInterviewerSkill>> GetSkills(int ProfileId);
        Task<IEnumerable<TransInterviewerCertificate>> GetCertificateList(int ProfileId);
        Task<IEnumerable<TransInterviewerEducation>> GetEducationList(int ProfileId);
        Task<IEnumerable<TransInterviewerProfessional>> GetProfessionalList(int ProfileId);
        Task<IEnumerable<TransInterviewerProjects>> GetProjectsList(int ProfileId);
        Task<IEnumerable<TransInterviewerAvailability>> GetAvailabilityList(int ProfileId);
        Task<IEnumerable<TransInterviewerAccDetails>> GetAccDetailsList(int ProfileId);
    }
}
