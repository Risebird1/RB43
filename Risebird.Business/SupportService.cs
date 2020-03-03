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
    public class SupportService : ISupportService
    {
        private ISupportRepository _supportRespository;
        private IApplicationErrorLogger _applicationErrorLogger;
        //private readonly INotificationHelper _notificationHelper;

        public SupportService(ISupportRepository supportRespository,IApplicationErrorLogger applicationErrorLogger)
        {
            _supportRespository = supportRespository;
            _applicationErrorLogger = applicationErrorLogger;
            
        }

        public List<Support> GetAllSupportHR()
        {
            try
            {
                return _supportRespository.GetAllSupportHR().ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public bool AddSupportHR(Support hrDetails)
        {
            try
            {
                return _supportRespository.AddSupportHR(hrDetails).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

    }
}
