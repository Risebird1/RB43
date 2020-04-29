using System.Collections.Generic;
using System.Threading.Tasks;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;

namespace Risebird.Business.Interface
{
    public interface IInterviewerService
    {
        List<InterviewerProfileVM> GetInterviewers();
        Task<bool> AddInterviewerProfile(InterviewerProfileVM interviewerProfile);
        Task<bool> EditInterviewerProfile(InterviewerProfileVM interviewerProfile);
        Task<bool> AddEmployerProfile(InterviewerProfessional interviewerProfessional);
        Task<bool> EditEmployerProfile(InterviewerProfessional interviewerProfessional);
        Task<bool> DeleteEmployerProfile(int id);

        //To retrieve the NickNames
        IEnumerable<string> GetNickNames(int UserId);
        IEnumerable<string> GetEmailIds(int UserId);
        InterviewerProfileVM GetInterviewerProfileById(int userid);
        InterviewerProfessional GetEmployerById(int Id);
        List<LevelTech> GetLevelTech();
        List<ExperienceRange> GetExperienceRange();
        List<JobFunction> GetJobFunction();
        List<Industry> GetIndustry();
        Task<bool> AddInterviewerMyProfile(InterviewerProfileVM interviewerProfile);
    }
}
