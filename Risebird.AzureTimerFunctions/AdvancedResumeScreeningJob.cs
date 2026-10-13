using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// Advanced Resume Screening: every 5 minutes, claims up to 20 candidates (on IRFs with the
    /// feature on and an approved rubric - new candidates included automatically) and scores them,
    /// at most 4 at a time.
    ///
    /// Claims are atomic (usp_GetCandidatesForAdvancedScreening), so this is safe to run in more
    /// than one instance, and alongside the web host's immediate processing. A row stuck in
    /// Processing for 30 minutes - an instance that died mid-batch - is claimed again.
    /// </summary>
    public class AdvancedResumeScreeningJob
    {
        private const int BatchSize = 20;
        private readonly IAdvancedScreeningService _advancedScreeningService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger log;

        public AdvancedResumeScreeningJob(ILoggerFactory loggerFactory, IAdvancedScreeningService advancedScreeningService,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _advancedScreeningService = advancedScreeningService;
            _applicationErrorLogger = applicationErrorLogger;
            log = loggerFactory.CreateLogger<AdvancedResumeScreeningJob>();
        }

        [Function("AdvancedResumeScreeningJob")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            log.LogInformation($"AdvancedResumeScreeningJob : started at {DateTime.Now}");
            try
            {
                var claimed = await _advancedScreeningService.ScreenPendingBatchAsync(BatchSize);
                log.LogInformation($"AdvancedResumeScreeningJob : screened {claimed} candidate(s)");
            }
            catch (Exception ex)
            {
                log.LogInformation($"AdvancedResumeScreeningJob : threw {ex.Message}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
