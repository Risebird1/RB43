using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IActivityTrackerService
    {
        Task<IEnumerable<ActivityTrackerVM>> GetAllActivities(DateTime startDate, DateTime endDate);
        Task<bool> SaveCaptureClickDetails(CapturePageDM model);
    }
}
