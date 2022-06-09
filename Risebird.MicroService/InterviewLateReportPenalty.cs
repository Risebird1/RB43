using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class InterviewLateReportPenalty
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingRespository _meetingRespository;

        public InterviewLateReportPenalty(IApplicationErrorLogger applicationErrorLogger, IMeetingRespository meetingRespository)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _meetingRespository = meetingRespository;
        }
        [FunctionName("InterviewLateReportPenalty")]
        public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer, ILogger log)
        {
            try
            {
                log.LogInformation($"C# Timer trigger function InterviewLateReportPenalty executed at: {DateTime.Now}");
                //Create a funcito to update the penalty details
                // if Interviewer not update the interview report after 8 hr of complete the meeting
                await _meetingRespository.ApplyLateReportPenalty();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
