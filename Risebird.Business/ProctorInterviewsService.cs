using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class ProctorInterviewsService : IProctorInterviewsService
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IProctorInterviewsRepository _proctorInterviewsRepository;

        public ProctorInterviewsService(INotificationHelper notificationHelper,  IApplicationErrorLogger applicationErrorLogger, IProctorInterviewsRepository proctorInterviewsRepository)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _proctorInterviewsRepository = proctorInterviewsRepository;

        }

        public async Task<IEnumerable<VMProperties>> GetAllVMs()
        {
            try
            {
                var result = await _proctorInterviewsRepository.GetAllVMs();
                return result;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IEnumerable<TriggeredVM>> GetAllTriggeredVMs()
        {
            try
            {
                var result = await _proctorInterviewsRepository.GetAllTriggeredVMs();
                return result;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> DeleteTriggeredVMs(List<int> ids)
        {
            try
            {
                var result = await _proctorInterviewsRepository.DeleteTriggeredVMs(ids);
                return result;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

    }
}
