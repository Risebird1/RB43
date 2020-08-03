using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class MeetingInviteService : IMeetingInviteService
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IMeetingInviteRepository _meetingInviteRepository;
        public MeetingInviteService(INotificationHelper notificationHelper, IApplicationErrorLogger applicationErrorLogger, IMeetingInviteRepository meetingInviteRepository)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _meetingInviteRepository = meetingInviteRepository;
        }

        /// <summary>
        /// Check if any Microsoft Team account is avialble and fetch account email
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public async Task<string> CheckMeetingAccount(DateTime startTime, DateTime endTime)
        {
            try
            {
                return await _meetingInviteRepository.CheckMeetingAccount(startTime, endTime);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }

        /// <summary>
        /// Insert MeetingInvite details
        /// </summary>
        /// <param name="starttime"></param>
        /// <param name="endtime"></param>
        /// <param name="Emailaddress"></param>
        /// <param name="Accountemail"></param>
        /// <param name="Linkurl"></param>
        /// <param name="whom"></param>
        /// <param name="Subject"></param>
        /// <returns></returns>
        public async Task<string> InsertMeetingInvite(DateTime starttime, DateTime endtime, string Emailaddress, string Accountemail, string Linkurl, string whom, string Subject)
        {
            try
            {
                return await _meetingInviteRepository.InsertMeetingInvite(starttime, endtime, Emailaddress, Accountemail, Linkurl, whom, Subject);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }
    }
}
