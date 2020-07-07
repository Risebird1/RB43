using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IInterviewerRepository
    {
        Task<IEnumerable<InterviewerProfileVM>> GetInterviewers();
        Task<bool> AddOrEditInterviewerProfile(InterviewerProfileVM interviewerProfile);
        Task<bool> DeleteInterviewerProfile(int UserId);
        Task<bool> AddEmployerProfile(InterviewerProfessional interviewerProfessional);
        Task<bool> EditEmployerProfile(InterviewerProfessional interviewerProfessional);
        Task<bool> DeleteEmployerProfile(int id);
        Task<List<InterviewerProfessional>> GetEmployerListById(int id);
        IEnumerable<string> GetNickNames(int UserId);
        IEnumerable<string> GetEmailIds(int UserId);
        IEnumerable<LevelTech> GetLevelTech();
        List<ExperienceRange> GetExperienceRange();
        List<JobFunction> GetJobFunction();
        List<Industry> GetIndustry();
        Task<IEnumerable<InterviewerProfileVM>> GetInterviewerProfileById(int userid);
        Task<InterviewerProfessional> GetEmployerById(int id);
        Task<IEnumerable<TransInterviewerSkill>> GetSkills(int ProfileId);
        Task<IEnumerable<TransInterviewerCertificate>> GetCertificateList(int ProfileId);
        Task<IEnumerable<TransInterviewerEducation>> GetEducationList(int ProfileId);
        Task<IEnumerable<InterviewerProfessional>> GetProfessionalList(int ProfileId);
        Task<IEnumerable<TransInterviewerProjects>> GetProjectsList(int ProfileId);
        Task<IEnumerable<TransInterviewerAvailability>> GetAvailabilityList(int ProfileId);
        Task<IEnumerable<TransInterviewerAccDetails>> GetAccDetailsList(int ProfileId);

        Task<bool> AddInterviewerMyProfile(InterviewerProfileVM interviewerProfile);
        IEnumerable<InterviewerInvites> GetInterviewerInvites(string emailId);
        Task<IEnumerable<Meeting>> GetUnapprovedInterviewerScheduledInterviews(string emailID);
        Task<IEnumerable<InterviewerProfileVM>> GetInterviewerProfileByEmailId(string emailId, string profileName);
        Task<IEnumerable<Meeting>> GetOpenInterviewerAssessment(string emailID);
        Task<IEnumerable<Meeting>> GetAssignedInterviewerAssessment(string emailID);
        string AssignYourself(int meetingid, string interviewerMail);
        Task<bool> UnassignYourself(int meetingid);
        MeetingVM GetMeetingById(int meetingid);
    }
}
