using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class ActivityTrackerService : IActivityTrackerService
    {
        private readonly IActivityTrackerRepository _activityTrackerRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public ActivityTrackerService(IActivityTrackerRepository activityTrackerRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _activityTrackerRepository = activityTrackerRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<IEnumerable<ActivityTrackerVM>> GetAllActivities(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _activityTrackerRepository.GetAllActivities(startDate, endDate);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> SaveCaptureClickDetails(CapturePageDM model)
        {
            try
            {
                return await _activityTrackerRepository.SaveCaptureClickDetails(model);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return false;
        }
    }
}
