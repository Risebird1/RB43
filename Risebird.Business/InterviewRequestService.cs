using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Risebird.Business
{
    public class InterviewRequestService : IInterviewRequestService
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IInterviewRequestRepository _interviewRequestRepository;
        private readonly IMeetingRespository _meetingRespository;
        private readonly INotificationHelper _notificationHelper;
        private readonly IIRFRepository _irfRepository;

        public InterviewRequestService(IApplicationErrorLogger applicationErrorLogger, IInterviewRequestRepository interviewRequestRepository, IMeetingRespository meetingRespository, INotificationHelper notificationHelper, IIRFRepository irfRepository)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _interviewRequestRepository = interviewRequestRepository;
            _meetingRespository = meetingRespository;
            _notificationHelper = notificationHelper;
            _irfRepository = irfRepository;
        }
        public bool CreateInerviewRequest(InterviewRequestVM meeting)
        {
            try
            {
                return _interviewRequestRepository.CreateInerviewRequest(meeting).Result;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public IEnumerable<InterviewRequestVM> GetAllInterviewRequests(string emailId, string profileName)
        {
            try
            {
                var irfList = _interviewRequestRepository.GetAllInterviewRequests(emailId, profileName).Result.ToList();
                return irfList;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public InterviewRequestVM FetchInterviewRequestById(int id)
        {
            try
            {
                var result = _interviewRequestRepository.FetchInterviewRequestById(id).Result;
                return result;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new InterviewRequestVM();
            }
        }
        public bool AssignInerviewRequest(int userid, int id)
        {
            try
            {
                return _interviewRequestRepository.AssignInerviewRequest(userid,id).Result;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public bool SetIRFCreated(int id)
        {
            try
            {
                return _interviewRequestRepository.SetIRFCreated(id).Result;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
    }
}
