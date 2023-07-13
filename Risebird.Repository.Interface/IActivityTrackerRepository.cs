using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IActivityTrackerRepository
    {
        Task<IEnumerable<ActivityTrackerVM>> GetAllActivities(DateTime startDate, DateTime endDate);
        Task<bool> AddActivity(ActivityTrackerVM model);
        Task<bool> SaveCaptureClickDetails(CapturePageDM model);
    }
}
