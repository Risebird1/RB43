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
        Task<IEnumerable<Meeting>> GetAllOpenInterviews(string emailId);
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
        //Task<InterviewReport> GetInteviewReport(int interviewId, int irfId);
    }
}
