using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboarRepository _dashboardRespository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public DashboardService(IDashboarRepository dashboardRespository, IApplicationErrorLogger applicationErrorLogger)
        {
            _dashboardRespository = dashboardRespository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<IEnumerable<Meeting>> GetDateWiseMeetings(DateTime date)
        {
            try
            {
                return _dashboardRespository.GetDateWiseMeetings(date).Result.OrderBy(x => x.interviewDate);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IEnumerable<DailyInterviewSlot>> GetDateWiseMeetingsSlots(DateTime date)
        {
            try
            {
                return _dashboardRespository.GetDateWiseMeetingsSlots(date).Result;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<DashboardInterviewerCount> GetInterviewerCount()
        {
            try
            {
                return await _dashboardRespository.GetInterviewerCount();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IEnumerable<IRF>> GetIRFProgress(DateTime date)
        {
            try
            {
                return await _dashboardRespository.GetIRFProgress(date);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public IEnumerable<IrfProgress> GetProgressOptions()
        {
            try
            {
                return _dashboardRespository.GetProgressOptions();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> UpdateIRFProgress(IList<IRF> irfProgressList)
        {
            try
            {
                return await _dashboardRespository.UpdateIRFProgress(irfProgressList);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<MeetingResultCount> GetMeetingResultCount()
        {
            try
            {
                return await _dashboardRespository.GetMeetingResultCount();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<CandidatesCount> GetCandidatesCount()
        {
            try
            {
                var countObj = await _dashboardRespository.GetCandidatesCount();
                countObj.TotalCandidates = (countObj.Selected + countObj.Rejected + countObj.BorderLine + countObj.Proxy + countObj.NoShowCount + countObj.DroppedCount + countObj.ScheduledCount + countObj.Pending);
                return countObj;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IEnumerable<Meeting>> GetDateWiseClientMeetings(DateTime date, string emailId)
        {
            try
            {
                return _dashboardRespository.GetDateWiseClientMeetings(date, emailId).Result.OrderBy(x => x.interviewDate);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IRFFunnel> GetClientFunnelData(string emailId)
        {
            try
            {
                return await _dashboardRespository.GetClientFunnelData(emailId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IEnumerable<ClientDailySlotsStatus>> GetDateWiseClientMeetingsSlots(DateTime date, string emailId)
        {
            try
            {
                return await _dashboardRespository.GetDateWiseClientMeetingsSlots(date, emailId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<CandidatesCount> GetClientCandidatesCount(string emailId)
        {
            try
            {
                var countObj = await _dashboardRespository.GetClientCandidatesCount(emailId);
                countObj.TotalCandidates = (countObj.Selected + countObj.Rejected + countObj.BorderLine + countObj.Proxy + countObj.NoShowCount + countObj.DroppedCount + countObj.ScheduledCount + countObj.Pending);
                return countObj;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<MeetingResultCount> GetClientMeetingsResults(string emailId)
        {
            try
            {
                return await _dashboardRespository.GetClientMeetingsResults(emailId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}
