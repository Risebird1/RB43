using Risebird.Entities.Data;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface ISettingsService
    {
        Task<Settings> GetAllSettings();
        Task<bool> UpdateSettings(Settings settings);
        Task<int> GetInterviewUnassignedMinutes();
    }
}
