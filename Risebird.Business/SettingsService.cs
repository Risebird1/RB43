using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class SettingsService : ISettingsService
    {
        private readonly ISettingsRepository _settingsRespository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public SettingsService(ISettingsRepository settingsRespository, IApplicationErrorLogger applicationErrorLogger)
        {
            _settingsRespository = settingsRespository;
            _applicationErrorLogger = applicationErrorLogger;

        }

        public async Task<Settings> GetAllSettings()
        {
            try
            {
               return await _settingsRespository.GetAllSettings();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> UpdateSettings(Settings settings)
        {
            try
            {
                return await _settingsRespository.UpdateSettings(settings);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<int> GetInterviewUnassignedMinutes()
        {
            try
            {
                return await _settingsRespository.GetInterviewUnassignedMinutes();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }


    }
}
