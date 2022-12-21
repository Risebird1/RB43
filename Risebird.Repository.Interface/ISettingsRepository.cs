using Risebird.Entities.Data;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ISettingsRepository
    {
        Task<Settings> GetAllSettings();
        Task<bool> UpdateSettings(Settings settings);
        Task<int> GetInterviewUnassignedMinutes();
    }
}
