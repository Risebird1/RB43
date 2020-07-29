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
        public List<Meeting> GetInvitecnt()
        {
            try
            {
                return _dashboardRespository.GetTotalInvites().ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<Meeting> GetInterviwercnt()
        {
            try
            {
                return _dashboardRespository.GetTotalInterviewer().ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<Meeting> GetCandidatecnt()
        {
            try
            {
                return _dashboardRespository.GetTotalcandidate().ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public string GetDoughnutGraphDetails(string flag)
        {
            try
            {
                if (_dashboardRespository.GetDoughnutGraphDetails(flag) == null)
                    return null;
                else

                    return _dashboardRespository.GetDoughnutGraphDetails(flag).Result;

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
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

        public Task<DashboardInterviewerCount> GetInterviewerCount()
        {
            try
            {
                return _dashboardRespository.GetInterviewerCount();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IEnumerable<IRF>> GetIRFProgress()
        {
            try
            {
                return await _dashboardRespository.GetIRFProgress();
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
                return false ;
            }
        }

    }
}
