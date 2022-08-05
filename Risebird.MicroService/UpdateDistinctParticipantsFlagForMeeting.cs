using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class UpdateDistinctParticipantsFlagForMeeting
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingRespository _meetingRespository;

        public UpdateDistinctParticipantsFlagForMeeting(IApplicationErrorLogger applicationErrorLogger, IMeetingRespository meetingRespository)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _meetingRespository = meetingRespository;
        }

        [FunctionName("UpdateDistinctParticipantsFlagForMeeting")]
        public async Task Run([TimerTrigger("0 */15 * * * *")] TimerInfo myTimer, ILogger log)
        {
            try
            {
                await _meetingRespository.UpdateSingleParticipantFlagForMeeting();
                log.LogInformation($"UpdateDistinctParticipantsFlagForMeeting : C# Timer trigger function executed at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                log.LogError($"UpdateDistinctParticipantsFlagForMeeting : Failed to excute function. Exception thrown at: {DateTime.Now}, Message : {ex.Message}");


            }
        }
    }
}
