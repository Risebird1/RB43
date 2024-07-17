using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface ITeamsMeetingHelper
    {
        Task<MetingResponse> CreateTeamsMeeting(string subject, DateTime MeetingDate, int interviewduration, string TimeZoneDetails);
        Task<string> GetTeamMeetingVideo(string meetingid);
    }
}
