using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IInstaInterviewRepository
    {
        Task<IEnumerable<InstaMeetingVM>> GetAllInstaRequests(string emailID, string profileName);
        Task<IEnumerable<InstaMeetingVM>> GetAllInterviewMeetings(DateTime startDate, DateTime endDate, int id, string emailId = null);
        Task<bool> CreateRequest(InstaMeetingVM meeting);
        Task<InstaMeetingVM> FetchRequestById(int id);
        Task<bool> AddInstaMeeting(InstaMeetingVM meeting);
        Task<InstaMeetingVM> GetInstaMeeting(int id);
        Task<bool> UpdateInstaMeeting(InstaMeetingVM meeting);
        string FetchSkillsByAssessmentGroup(int id);
        Task<IEnumerable<InstaMeetingVM>> GetAllInstaOpenInterviews(string emailId);
        string InstaAssignYourself(int meetingid, string interviewerMail);
        Task<IEnumerable<InstaMeetingVM>> GetAllInstaAssignedInterviews(string emailID);
        List<string> FetchInstaIntervieweByAssessmentGroup(string Assessmentgroupid);
        Task<InstaMeetingVM> GetInstaInteviewReport(int interviewid);
        Task<bool> AddInstaInterviewReport(InstaMeetingVM reportDetails);
        string UpdateInstaZoomLink(int meetingid, string Zoomlink, string accountEmail);
        Task<string> CheckInstaMeetingAccount(DateTime starttime, DateTime endtime);
        string UpdateInstaZoomDetails(DateTime starttime, DateTime endtime, string Accoruntemail);
        string CheckForInstaAssign(int meetingid);
        Task<int> GetClientPasswordResetStatus(string emailId);
    }
}
