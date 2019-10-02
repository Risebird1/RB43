using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IMeetingService
    {
        List<MeetingVM> GetMeetingList();
        bool AddMeeting(MeetingVM meeting);
        bool UpdateMeeting(MeetingVM meetingToUpdate);

        MeetingVM GetMeeting(int id);
        List<Meeting> GetAllOpenInterviews(string emailID);
        List<Meeting> GetAllQCInterviews(string emailID);
        List<MeetingVM> GetAllClientInterviews(string emailID);

        string AssignYourself(int meetingid, string interviewerMail, DateTime interviewDate);
        bool UnassignYourself(int meetingid, string email);
        List<Meeting> GetAllAssignedInterviews(string emailID);
        bool UpdateComments(InterviewComments comments);
        InterviewComments GetInterviewComments(int interviewId);
        Task<InterviewReport> GetInteviewReport(int interviewId, int irfId);
        Task<bool> AddInterviewReport(InterviewReport reportModel);
        IEnumerable<SkillComment> GetTechSkillComments(int IRFid);
        SelectList GetIRFList();
        IEnumerable<TransClientUser> GetClientsByRoles(string IRF);
        List<MeetingVM> GetClientMeetingList(string emailId);
        bool PostApproval(string imid, string value, string comment);
        bool PostSelectionStatus(string imid, string value, string comment);
        bool SaveClientFeedback(string imid, string isApproved, string approvalComment, string isSelected, string selectionComment, string joiningStatus, string joiningComment, string offerStatus, string offerComment);
    }
}
