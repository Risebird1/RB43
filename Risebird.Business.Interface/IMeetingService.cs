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
    }
}
