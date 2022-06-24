using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IRBMeetHelper
    {
        Task<RBMeetCreateMeetingResponse> CreateMeeting(string agentname, string visitorName, DateTime datetime);
    }
}
