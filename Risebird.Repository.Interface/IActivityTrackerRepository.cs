using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IActivityTrackerRepository
    {
        Task<IEnumerable<ActivityTrackerVM>> GetAllActivities();
        Task<bool> AddActivity(ActivityTrackerVM model);
    }
}
