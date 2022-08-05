using Risebird.Entities.Data;
using System;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IRBMeetHelper
    {
        Task<RBMeetCreateMeetingResponse> CreateMeeting(string agentname, string visitorName, DateTime datetime);
    }
}
