using System;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;
using System.Collections.Generic;
using System.Linq;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class MeetingService : IMeetingService
    {
        private IMeetingRespository _meetingRespository;
        private IApplicationErrorLogger _applicationErrorLogger;
        public MeetingService(IMeetingRespository meetingRespository, IApplicationErrorLogger applicationErrorLogger)
        {
            _meetingRespository = meetingRespository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public List<Meeting> GetMeetingList()
        {
            try
            {
                return  _meetingRespository.GetAllInterviewMeetings().Result.OrderByDescending(x => x.dtCreatedOn).ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public bool AddMeeting(MeetingVM meeting)
        {
            try
            {
                _meetingRespository.AddMeeting(meeting);
                return true;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public List<Meeting> GetAllOpenInterviews()
        {
            try
            {
               var openMeeting= _meetingRespository.GetAllOpenInterviews().Result.OrderByDescending(x => x.dtCreatedOn).ToList();
               return openMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}





