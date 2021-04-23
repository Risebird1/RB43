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
        public SelectList GetISCList()
        {
            try
            {
                return new SelectList(_unapprovedInterviewerMeetingsRepository.GetISCList(), "InterviewerSkillId", "InterviewerSkillId");
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool AddorupdateMeeting(InterviewerMeetingVM meeting)
        {
            try
            {
                if (_unapprovedInterviewerMeetingsRepository.AddorupdateMeeting(meeting).Result)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public InterviewerMeetingVM GetMeeting(int id)
        {
            try
            {
                var meeting = _unapprovedInterviewerMeetingsRepository.GetInterviewMeetingById(id);
                if (meeting == null)
                    return null;
                else
                    return meeting.Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
    }
}
