using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
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

        public async Task<IEnumerable<ActivityTrackerVM>> GetAllActivities()
        {
            try
            {
                return await _activityTrackerRepository.GetAllActivities();
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
    }
}
