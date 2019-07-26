using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IMeetingService
    {
        List<Meeting> GetMeetingList();
        bool AddMeeting(MeetingVM meeting);
        List<Meeting> GetAllOpenInterviews();
        string AssignYourself(int meetingid, string interviewerMail);
        bool UnassignYourself(int meetingid);
        List<Meeting> GetAllAssignedInterviews();
        bool UpdateComments(InterviewComments comments);
        InterviewComments GetInterviewComments(int interviewId);
        Task<InterviewReport> GetInteviewReport(int interviewId, int irfId);
        Task<bool> AddInterviewReport(InterviewReport reportModel);
    }
}
