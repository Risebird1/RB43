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
    }
}
