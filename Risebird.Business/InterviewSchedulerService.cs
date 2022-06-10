using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class InterviewSchedulerService : IInterviewSchedulerService
    {
        private readonly IInterviewSchedulerRepository _interviewSchedulerRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public InterviewSchedulerService(IInterviewSchedulerRepository interviewSchedulerRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _interviewSchedulerRepository = interviewSchedulerRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }
        public List<daytimes_report> FetchEventList()
        {
            try
            {
                return _interviewSchedulerRepository.FetchEventList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public bool CreateEvent(daytimes_report model)
        {
            try
            {
                return _interviewSchedulerRepository.CreateEvent(model);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public daytimes_report FetchEventById(int eventid)
        {
            try
            {
                return _interviewSchedulerRepository.FetchEventById(eventid);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<daytimes_second> FetchTimeslotsByEventId(int eventid)
        {
            try
            {
                return _interviewSchedulerRepository.FetchTimeslotsByEventId(eventid);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<daytimes_second> FetchDetailsByUniqueId(string uniqueid)
        {
            try
            {
                return _interviewSchedulerRepository.FetchDetailsByUniqueId(uniqueid);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public bool DeactivateEventById(int eventid, int status)
        {
            try
            {
                return _interviewSchedulerRepository.DeactivateEventById(eventid, status);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public SelectList GetIRFList()
        {
            try
            {
                var results = _interviewSchedulerRepository.GetIRFList();
                return new SelectList(results.Where(x => x.EndDate > DateTime.Now), "IRFId", "IRFId");
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<daytimes_second> FetchSlots(DateTime value, string uniqueid)
        {
            try
            {
                return _interviewSchedulerRepository.FetchSlots(value, uniqueid);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<string> CheckInstaMeetingAccount(DateTime starttime, DateTime endtime)
        {
            try
            {
                return await _interviewSchedulerRepository.CheckInstaMeetingAccount(starttime, endtime);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<string> FetchJDTitle(string IRFId)
        {
            try
            {
                return await _interviewSchedulerRepository.FetchJDTitle(IRFId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public string UpdateInstaZoomDetails(DateTime starttime, DateTime endtime, string Accountemail)
        {
            try
            {
                return _interviewSchedulerRepository.UpdateInstaZoomDetails(starttime, endtime, Accountemail);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public ScheculeInterviewIRFVM fetchIRFClientDetails(string IRFId)
        {
            try
            {
                return _interviewSchedulerRepository.fetchIRFClientDetails(IRFId);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public int AutoCreateEvent(daytimes_report model)
        {
            try
            {
                return _interviewSchedulerRepository.AutoCreateEvent(model);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }
        public int FetchEventidByUniqueid(string _uniqueid)
        {
            try
            {
                return _interviewSchedulerRepository.FetchEventidByUniqueid(_uniqueid);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }
        public bool UpdateSchedulingLinkDetails(string _uniqueid)
        {
            try
            {
                return _interviewSchedulerRepository.UpdateSchedulingLinkDetails(_uniqueid);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<bool> CheckInterviewConcurrency(DateTime interviewdate, string IRFId, string Uniqueid)
        {
            try
            {
                return await _interviewSchedulerRepository.CheckInterviewConcurrency(interviewdate, IRFId, Uniqueid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
    }
}
