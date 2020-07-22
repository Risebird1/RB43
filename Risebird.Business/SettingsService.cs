using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;

namespace Risebird.Business
{
    public class SettingsService : ISettingsService
    {
        private ISettingsRepository _settingsRespository;
        private IApplicationErrorLogger _applicationErrorLogger;
        //private readonly INotificationHelper _notificationHelper;

        public SettingsService(ISettingsRepository settingsRespository, IApplicationErrorLogger applicationErrorLogger)
        {
            _settingsRespository = settingsRespository;
            _applicationErrorLogger = applicationErrorLogger;

        }

        public Settings GetAllSettings()
        {
            try
            {
                return _settingsRespository.GetAllSettings();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public bool UpdateSettings(Settings settings)
        {
            try
            {
                return _settingsRespository.UpdateSettings(settings);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public int GetInterviewUnassignedMinutes()
        {
            try
            {
                return _settingsRespository.GetInterviewUnassignedMinutes();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }


    }
}
