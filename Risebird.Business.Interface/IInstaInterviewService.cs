using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IInstaInterviewService
    {
        IEnumerable<InstaMeetingVM> GetAllInstaRequests(string emailId, string profileName);
        bool CreateRequest(InstaMeetingVM meeting);
        InstaMeetingVM FetchRequestById(int id);
        List<InstaMeetingVM> GetAllMeetingList(DateTime startDate, DateTime endDate, int id, string emailId = null);
        bool AddInstaMeeting(InstaMeetingVM meeting);
        Task<InstaMeetingVM> GetInstaMeeting(int id);
        bool UpdateInstaMeeting(InstaMeetingVM meeting);
        string FetchSkillsByAssessmentGroup(int id);
        List<InstaMeetingVM> GetAllInstaOpenInterviews(string emailID);
        string InstaAssignYourself(int meetingid, string interviewerMail, DateTime InterviewDate);
        List<InstaMeetingVM> GetAllInstaAssignedInterviews(string emailID);
        Task<InstaMeetingVM> GetInstaInteviewReport(int interviewId);
        Task<bool> AddInstaInterviewReport(InstaMeetingVM reportModel, string emailId);
        string UpdateInstaZoomLink(int meetingid, string Zoomlink, DateTime starttime, DateTime endtime, string accountEmail);
        Task<string> CheckInstaMeetingAccount(DateTime starttime, DateTime endtime);
        string CheckForInstaAssign(int meetingid);
        Task<int> GetClientPasswordResetStatus(string emailId);
    }
}
