using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Business
{
    public class DashboardService : IDashboardService
    {
        private IDashboarRepository _dashboardRespository;
        private IApplicationErrorLogger _applicationErrorLogger;
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
    }
}
