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
        Task<IEnumerable<Meeting>> GetAllOpenInterviews();
        string AssignYourself(int meetingid, string interviewerMail);
        Task<bool> UnassignYourself(int meetingid);
        Task<IEnumerable<Meeting>> GetAllAssignedInterview();
        Task<bool> UpdateComments(InterviewComments interviewComments);
        Task<InterviewComments> GetInterviewComments(int interviewId);
    }
}
