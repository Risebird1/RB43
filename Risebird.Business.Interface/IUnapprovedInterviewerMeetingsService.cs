using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IUnapprovedInterviewerMeetingsService
    {
        Task<IEnumerable<MeetingVM>> GetAllUnapprovedInterviewerMeetings(DateTime startDate, DateTime endDate);
        SelectList GetISCList();
        bool AddorupdateMeeting(InterviewerMeetingVM meeting);
        InterviewerMeetingVM GetMeeting(int id);
    }
}
