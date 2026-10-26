using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Threading.Tasks;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// Azure Timer Function — dials PlivoCX campaigns that are Running ("Start automatic").
    /// Every 15 seconds it refills free lines: the account's campaign line budget
    /// (PlivoCX:CampaignConcurrencyBudget) is shared between the Running campaigns inside their
    /// calling window, each up to its own Concurrent limit, and dialled with candidates whose Min
    /// gap has passed and who are under Attempts/day and Total attempts. Campaigns past EndsOn, or
    /// with nobody left Pending, become Completed. A pass with no Running campaign is one query.
    /// Timer runs never overlap (the Functions host holds a singleton lock per timer).
    ///
    /// The limits that PlivoCXInterviewSchedulingJob / PlivoCXRescheduleJob hard-code (9-22h,
    /// 4/day, 2h cooldown) come from each campaign's calling policy here. Those two jobs skip
    /// campaign candidates, and PlivoCX:LegacyTimerJobsEnabled = false turns them off.
    /// </summary>
    public class PlivoCXCampaignScheduler
    {
        private readonly IPlivoCXCampaignService  _campaignService;
        private readonly IApplicationErrorLogger  _applicationErrorLogger;
        private readonly ILogger                  _log;

        public PlivoCXCampaignScheduler(
            ILoggerFactory           loggerFactory,
            IPlivoCXCampaignService  campaignService,
            IApplicationErrorLogger  applicationErrorLogger)
        {
            _campaignService        = campaignService;
            _applicationErrorLogger = applicationErrorLogger;
            _log = loggerFactory.CreateLogger<PlivoCXCampaignScheduler>();
        }

        [Function("PlivoCXCampaignScheduler")]
        public async Task Run([TimerTrigger("*/15 * * * * *")] TimerInfo myTimer)
        {
            _log.LogInformation($"[PlivoCX-Campaign] Scheduler tick at {DateTime.UtcNow:u}");
            try
            {
                await _campaignService.RunSchedulerTickAsync();
            }
            catch (Exception ex)
            {
                _log.LogError($"[PlivoCX-Campaign] Scheduler exception: {ex.Message}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
