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
    /// Azure Timer Function — sends WhatsApp campaigns that are Running ("Start automatic"). Every 5 minutes,
    /// for each Running campaign: past EndsOn → Completed; otherwise settle who booked or confirmed, move Sent
    /// candidates past the re-send gap back to Pending (or to No response once every send is used), and inside
    /// the sending window send to up to BatchSize due candidates. A campaign with nobody left Pending or Sent
    /// becomes Completed.
    ///
    /// Off until WhatsAppCampaign:SchedulerEnabled = true (set it once the templates are live in Interakt).
    /// Timer runs never overlap (the Functions host holds a singleton lock per timer), and each row is claimed
    /// with a lock, so Send now on the portal never double-sends with a pass.
    /// </summary>
    public class WhatsAppCampaignScheduler
    {
        private readonly IWhatsAppCampaignService _campaignService;
        private readonly IApplicationErrorLogger  _applicationErrorLogger;
        private readonly ILogger                  _log;

        public WhatsAppCampaignScheduler(
            ILoggerFactory           loggerFactory,
            IWhatsAppCampaignService campaignService,
            IApplicationErrorLogger  applicationErrorLogger)
        {
            _campaignService        = campaignService;
            _applicationErrorLogger = applicationErrorLogger;
            _log = loggerFactory.CreateLogger<WhatsAppCampaignScheduler>();
        }

        [Function("WhatsAppCampaignScheduler")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            if (!_campaignService.SchedulerEnabled) return;
            _log.LogInformation($"[WhatsApp-Campaign] Scheduler tick at {DateTime.UtcNow:u}");
            try
            {
                await _campaignService.RunSchedulerTickAsync();
            }
            catch (Exception ex)
            {
                _log.LogError($"[WhatsApp-Campaign] Scheduler exception: {ex.Message}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
