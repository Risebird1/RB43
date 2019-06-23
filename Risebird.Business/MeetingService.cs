using Risebird.Business.Interface;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Business
{
    public class MeetingService : IMeetingService
    {
        private IMeetingRespository _meetingRespository;

        public MeetingService(IMeetingRespository meetingRespository)
        {
            _meetingRespository = meetingRespository;
        }

        public List<Meeting> GetMeetingList()
        {
            return _meetingRespository.GetAllInterviewMeetings().OrderByDescending(x=> x.dtCreatedOn).ToList();
        }
    }
}





