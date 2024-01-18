using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class AzureStorageService: IAzureStorageService
    {
        private readonly IAzureStorageRepository _azureStorageRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public AzureStorageService(IAzureStorageRepository azureStorageRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _azureStorageRepository = azureStorageRepository;
                _applicationErrorLogger = applicationErrorLogger;
        }
        public async Task<bool> SaveAzurStorageDetails(AzureStorageInfoModelVM model)
        {
            try
            {
                return await _azureStorageRepository.SaveAzurStorageDetails(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
    }
}
