using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IInstaInterviewRepository
    {
        Task<IEnumerable<InstaMeetingVM>> GetAllInterviewMeetings(DateTime startDate, DateTime endDate, string emailId = null);
        Task<bool> AddInstaMeeting(InstaMeetingVM meeting);
        Task<InstaMeetingVM> GetInstaMeeting(int id);
        Task<bool> UpdateInstaMeeting(InstaMeetingVM meeting);
        string FetchSkillsByAssessmentGroup(int id);
        Task<IEnumerable<InstaMeetingVM>> GetAllInstaOpenInterviews(string emailId);
        string InstaAssignYourself(int meetingid, string interviewerMail);
        Task<IEnumerable<InstaMeetingVM>> GetAllInstaAssignedInterviews(string emailID);
        List<string> FetchInstaIntervieweByAssessmentGroup(string Assessmentgroupid);
    }
}
