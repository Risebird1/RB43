using Risebird.Entities.Data;

namespace Risebird.Repository.Interface
{
    public interface ISettingsRepository
    {
        Settings GetAllSettings();
        bool UpdateSettings(Settings settings);
        int GetInterviewUnassignedMinutes();
    }
}
