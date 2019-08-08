using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IMeetingRespository
    {
        Task<IEnumerable<Meeting>> GetAllInterviewMeetings();
        Task<bool> AddMeeting(MeetingVM meeting);
        Task<MeetingVM> GetInterviewMeetingById(int id);
        Task<bool> UpdateMeeting(MeetingVM meeting);

        Task<IEnumerable<Meeting>> GetAllOpenInterviews(string emailId);
        Task<IEnumerable<Meeting>> GetAllQCInterviews(string emailId);
        Task<IEnumerable<MeetingVM>> GetAllClientInterviews(string emailId);
        
        string AssignYourself(int meetingid, string interviewerMail);
        Task<bool> UnassignYourself(int meetingid);
        Task<IEnumerable<Meeting>> GetAllAssignedInterview(string emailId);
        Task<bool> UpdateComments(InterviewComments interviewComments);
        Task<InterviewComments> GetInterviewComments(int interviewId);
        IEnumerable<TransJoinability> GetTransJoinability(int id);
        IEnumerable<TransSkill> GetSkillsbyIRFid(int id);
        IEnumerable<TransSkillGeneral> GetSkillsGeneralbyIRFid(int id);
        IEnumerable<SkillComment> GetTechSkillComments(int id);
        IEnumerable<Level> GetLevels();
        IEnumerable<Skill> GetSkills();
        Task<bool> AddInterviewReport(InterviewReport reportDetails);
        IEnumerable<TransSelectability> GetTransSelectability(int id);
        IEnumerable<InterviewReport> GetInterviewReportById(int id);

        IEnumerable<JoinabilityReport> GetJoinabilityReport(int id);
        IEnumerable<SelectabilityReport> GetSelectabilityReport(int id);
        IEnumerable<QuestionsDiscussed> GetQuestionsDiscussed(int id);
        IEnumerable<TechSkillReport> GetTechSkillReport(int id);
        IEnumerable<SkillGeneralReport> GetSkillGeneralReport(int id);
        ProctoringReport GetProctoringReport(int id);
        IEnumerable<LevelTech> GetLevelTech();
        IEnumerable<string> GetIRFName(int id);
        //Task<InterviewReport> GetInteviewReport(int interviewId, int irfId);
    }
}
