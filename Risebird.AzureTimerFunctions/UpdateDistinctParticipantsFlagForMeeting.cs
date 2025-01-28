using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;

namespace Risebird.AzureTimerFunctions
{
    public class UpdateDistinctParticipantsFlagForMeeting
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingRespository _meetingRespository;
        private readonly ILogger log;

        public UpdateDistinctParticipantsFlagForMeeting(ILoggerFactory loggerFactory, IApplicationErrorLogger applicationErrorLogger, IMeetingRespository meetingRespository)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _meetingRespository = meetingRespository;
            log = loggerFactory.CreateLogger<UpdateDistinctParticipantsFlagForMeeting>();
        }

        [Function("UpdateDistinctParticipantsFlagForMeeting")]
        public async Task Run([TimerTrigger("0 */15 * * * *")] TimerInfo myTimer)
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
