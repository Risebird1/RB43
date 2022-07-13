using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Business
{
    public class SupportService : ISupportService
    {
        private readonly ISupportRepository _supportRespository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public SupportService(ISupportRepository supportRespository, IApplicationErrorLogger applicationErrorLogger)
        {
            _supportRespository = supportRespository;
            _applicationErrorLogger = applicationErrorLogger;

        }

        public List<Support> GetAllSupportHR(string emailId = null)
        {
            try
            {
                return _supportRespository.GetAllSupportHR(emailId).ToList();
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

        public Support GetSupportHR(int id)
        {
            try
            {
                return _supportRespository.GetSupportHR(id);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool UpdateSupportHR(Support hrDetails)
        {
            try
            {
                return _supportRespository.UpdateSupportHR(hrDetails);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public bool DeleteSupportHR(int id)
        {
            try
            {
                return _supportRespository.DeleteSupportHR(id);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
    }
}
