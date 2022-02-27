using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        Task<InterviewerProfileVM> GetInterviewerProfileByEmailId(string emailId, string profile);
        List<Meeting> GetOpenInterviewerAssessment(string emailId);
        List<Meeting> GetAssignedInterviewerAssessment(string emailID);
        string AssignYourself(int meetingid, string interviewerMail, DateTime interviewDate);
        bool UnassignYourself(int meetingid, string email);
        Task<InterviewReport> GetInterviewerinteviewReport(int interviewId, int irfId);
        Task<bool> AddInterviewerReport(InterviewReport reportModel);
        IEnumerable<SkillComment> GetTechSkillComments(int IRFid);
        IEnumerable<AssignmentVM> GetInterviewerAssessment(string emailid);
        string AddAssessment(AssignmentVM _assessment);
        IEnumerable<AssignmentVM> FetchAssessmentGroups();
        string ChangeStatusAssessment(int assessmentid, int status);
        string UpdateAssessment(AssignmentVM _assessment);
        Task<IEnumerable<string>> GetAllTagsFromIRF();
        Task<InterviewReport> GetInterviewerRecommendationDetails(int Userid);
        Task<bool> InterviewerRecommendation(InterviewReport reportModel);
        Task<TransInterviewerAccDetails> fetchBankDetails(int interviewerid);
        Task<List<TransInterviewerAvailability>> fetchAvailDetails(int interviewerid);
        string GetUserRolebyEmailid(string Emailid);
        Task<List<InterviewerRating>> fetchClientFeedbackbyInterviewer(string emailid);
        Task<List<CommonInterviewDetailsVM>> FetchAllInterviewerallUpcomingInterview();
        Task<List<string>> FetchAllPasswordResetDetails(int noofdays);
        Task<List<TdsCertificateReport>> FetchTdsCertificatesbyEmailid(string Emailid);
        bool AddTdsCertificates(TdsCertificateReport model);
        TdsCertificateReport FetchTdsCertificatebyid(int certificateid);
        Task<IEnumerable<IRFInterviewerMappingVM>> IRFInterviewerMappingByInterviewerid(string emailid);
        Task<IEnumerable<IRFInterviewerMembers>> GetAllInterviewerMembers();
        bool UpdateAllIntervieweravailbilityStatus(int status, string uniqueid);
    }
}
