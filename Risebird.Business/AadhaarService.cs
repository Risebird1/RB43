using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class AadhaarService : IAadhaarService
    {
        private readonly IAadhaarRepository _aadhaarRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public AadhaarService(IAadhaarRepository aadhaarRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _aadhaarRepository = aadhaarRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }
        public async Task<bool> SaveAadhaarDetails(AadhaarData model)
        {
            try
            {
                return await _aadhaarRepository.SaveAadhaarDetails(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
    }
}
