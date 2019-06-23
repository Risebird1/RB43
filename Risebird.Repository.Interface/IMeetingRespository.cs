using Risebird.Entities.Data;
using System;
using System.Collections.Generic;

namespace Risebird.Repository.Interface
{
    public interface IMeetingRespository
    {
        IEnumerable<Meeting> GetAllInterviewMeetings();
    }
}
