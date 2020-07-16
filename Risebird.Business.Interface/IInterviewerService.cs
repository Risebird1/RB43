using System;
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
        Task<bool> DeleteInterviewerProfile(int id);
        Task<bool> AddEmployerProfile(InterviewerProfessional interviewerProfessional);
        Task<bool> EditEmployerProfile(InterviewerProfessional interviewerProfessional);
        Task<bool> DeleteEmployerProfile(int id);

        //To retrieve the NickNames
        IEnumerable<string> GetNickNames(int UserId);
        IEnumerable<string> GetEmailIds(int UserId);
        InterviewerProfileVM GetInterviewerProfileById(int userid);
        InterviewerProfileVM GetInterviewerMyProfileById(int userid);
        InterviewerProfessional GetEmployerById(int Id);
        List<LevelTech> GetLevelTech();
        List<ExperienceRange> GetExperienceRange();
        List<JobFunction> GetJobFunction();
        List<Industry> GetIndustry();
        Task<bool> AddInterviewerMyProfile(InterviewerProfileVM interviewerProfile);
        IEnumerable<InterviewerInvites> GetInterviewerInvites(string emailId);
        List<Meeting> GetUnapprovedInterviewerScheduledInterviews(string emailID);
        InterviewerProfileVM GetInterviewerProfileByEmailId(string emailId, string profile);
        List<Meeting> GetOpenInterviewerAssessment(string emailId);
        List<Meeting> GetAssignedInterviewerAssessment(string emailID);
        string AssignYourself(int meetingid, string interviewerMail, DateTime interviewDate);
        bool UnassignYourself(int meetingid, string email);
        // Task<InterviewReport> GetInterviewerinteviewReport(int interviewId, int irfId);
        Task<bool> AddInterviewerReport(InterviewReport reportModel);
    }
}
