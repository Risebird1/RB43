using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class UnapprovedInterviewerMeetingsService : IUnapprovedInterviewerMeetingsService
    {
        private readonly IUnapprovedInterviewerMeetingsRepository _unapprovedInterviewerMeetingsRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;

        public UnapprovedInterviewerMeetingsService(IUnapprovedInterviewerMeetingsRepository unapprovedInterviewerMeetingsRepository, INotificationHelper notificationHelper, IApplicationErrorLogger applicationErrorLogger)
        {
            _unapprovedInterviewerMeetingsRepository = unapprovedInterviewerMeetingsRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;

        }

        public async Task<IEnumerable<MeetingVM>> GetAllUnapprovedInterviewerMeetings(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _unapprovedInterviewerMeetingsRepository.GetAllUnapprovedInterviewerMeetings(startDate, endDate);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<SelectList> GetISCList()
        {
            try
            {
                return new SelectList(await _unapprovedInterviewerMeetingsRepository.GetISCList(), "InterviewerSkillId", "InterviewerSkillId");
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> AddorupdateMeeting(InterviewerMeetingVM meeting)
        {
            try
            {
                if (await _unapprovedInterviewerMeetingsRepository.AddorupdateMeeting(meeting))
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<InterviewerMeetingVM> GetMeeting(int id)
        {
            try
            {
                var meeting = await _unapprovedInterviewerMeetingsRepository.GetInterviewMeetingById(id);
                if (meeting == null)
                    return null;
                else
                    return meeting;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
    }
}
