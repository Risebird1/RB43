using Risebird.Entities.Data;

namespace Risebird.Business.Interface
{
    public interface ISettingsService
    {
        Settings GetAllSettings();
        bool UpdateSettings(Settings settings);
        int GetInterviewUnassignedMinutes();
    }
}
