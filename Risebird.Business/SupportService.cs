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
    public class SupportService : ISupportService
    {
        private readonly ISupportRepository _supportRespository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public SupportService(ISupportRepository supportRespository, IApplicationErrorLogger applicationErrorLogger)
        {
            _supportRespository = supportRespository;
            _applicationErrorLogger = applicationErrorLogger;

        }

        public async Task<List<Support>> GetAllSupportHR(string emailId = null)
        {
            try
            {
                return await _supportRespository.GetAllSupportHR(emailId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> AddSupportHR(Support hrDetails)
        {
            try
            {
                return await _supportRespository.AddSupportHR(hrDetails);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<Support> GetSupportHR(int id)
        {
            try
            {
                return await _supportRespository.GetSupportHR(id);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> UpdateSupportHR(Support hrDetails)
        {
            try
            {
                return await _supportRespository.UpdateSupportHR(hrDetails);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> DeleteSupportHR(int id)
        {
            try
            {
                return await _supportRespository.DeleteSupportHR(id);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
    }
}
