using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IUnapprovedInterviewerMeetingsRepository
    {
        Task<IEnumerable<MeetingVM>> GetAllUnapprovedInterviewerMeetings(DateTime startDate, DateTime endDate);
        Task<IEnumerable<InterviewerSkillsVM>> GetISCList();
        Task<bool> AddorupdateMeeting(InterviewerMeetingVM meeting);
        Task<InterviewerMeetingVM> GetInterviewMeetingById(int id);
    }
}
