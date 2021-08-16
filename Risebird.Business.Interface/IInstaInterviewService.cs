using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IInstaInterviewService
    {
        List<InstaMeetingVM> GetAllMeetingList(DateTime startDate, DateTime endDate, string emailId = null);
        bool AddInstaMeeting(InstaMeetingVM meeting);
        Task<InstaMeetingVM> GetInstaMeeting(int id);
        bool UpdateInstaMeeting(InstaMeetingVM meeting);
        string FetchSkillsByAssessmentGroup(int id);
        List<InstaMeetingVM> GetAllInstaOpenInterviews(string emailID);
        string InstaAssignYourself(int meetingid, string interviewerMail, DateTime InterviewDate);
        List<InstaMeetingVM> GetAllInstaAssignedInterviews(string emailID);
    }
}
