using System.Collections.Generic;
using System.Threading.Tasks;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;

namespace Risebird.Business.Interface
{
    public interface IUserService
   {
       List<Menu> GetMenuBasedOnUserEmailld(string emailId);
       List<UserList> GetRisebirdUsers();
       List<ClientUserVM> GetClientUsers();
       List<InterviewerVM> GetInterviewers();
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

        // Interviewe Profile services
        //Task<bool> AddInterviewerProfile(InterviewerProfileVM interviewerProfile);
        //Task<bool> EditInterviewerProfile(InterviewerProfileVM interviewerProfile);
        //Task<bool> AddEmployerProfile(InterviewerProfessional interviewerProfessional);
        //Task<bool> EditEmployerProfile(InterviewerProfessional interviewerProfessional);
        //Task<bool> DeleteEmployerProfile(int id);

        ////To retrieve the NickNames
        //IEnumerable<string> GetNickNames(int UserId);
        //IEnumerable<string> GetEmailIds(int UserId);
        //InterviewerProfileVM GetInterviewerProfileById(int userid);
        //InterviewerProfessional GetEmployerById(int Id);
        //List<LevelTech> GetLevelTech();
        //List<ExperienceRange> GetExperienceRange();
        //List<JobFunction> GetJobFunction();
        //List<Industry> GetIndustry();

    }
}
