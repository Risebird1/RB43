using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// Runs every 10 minutes. For each AG email campaign that was created more than 1 hour ago
    /// and hasn't had batch calls triggered yet, this job:
    ///   1. Finds interviewers who haven't responded to the campaign email (no TransInterviewer entry)
    ///   2. Creates an InterviewerEmailAvailability record for each (Source = "Campaign - {CampaignName}")
    ///   3. Triggers a Plivo CX availability call
    ///   4. Marks the campaign as batch-call-triggered so it isn't processed again
    ///
    /// The resulting EA records are subject to the same auto-closure pipeline as all other
    /// availability records (resume fitment → AG assignment → close).
    /// </summary>
    public class CampaignBatchCallJob
    {
        private readonly IConfiguration                          _configuration;
        private readonly IInterviewerEmailAvailabilityRepository _eaRepository;
        private readonly IPlivoCXService                         _plivoCXService;
        private readonly IApplicationErrorLogger                 _errorLogger;
        private readonly ILogger                                 _log;

        public CampaignBatchCallJob(
            ILoggerFactory                          loggerFactory,
            IConfiguration                          configuration,
            IInterviewerEmailAvailabilityRepository eaRepository,
            IPlivoCXService                         plivoCXService,
            IApplicationErrorLogger                 errorLogger)
        {
            _configuration = configuration;
            _eaRepository  = eaRepository;
            _plivoCXService = plivoCXService;
            _errorLogger   = errorLogger;
            _log = loggerFactory.CreateLogger<CampaignBatchCallJob>();
        }

        [Function("CampaignBatchCallJob")]
        public async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer)
        {
            _log.LogInformation("[CampaignBatchCall] Job is currently DISABLED to control Plivo costs. Skipping.");
            return;

            _log.LogInformation($"[CampaignBatchCall] Triggered at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            try
            {
                var utcNow = DateTime.UtcNow;
                var istHour = utcNow.AddHours(5).AddMinutes(30).Hour;
                if (istHour < 9 || istHour >= 21)
                {
                    _log.LogInformation($"[CampaignBatchCall] Outside business hours (IST {istHour}:xx). Skipping.");
                    return;
                }

                var campaigns = await _eaRepository.GetCampaignsEligibleForBatchCallAsync();
                if (campaigns == null || !campaigns.Any())
                {
                    _log.LogInformation("[CampaignBatchCall] No eligible campaigns.");
                    return;
                }

                _log.LogInformation($"[CampaignBatchCall] Found {campaigns.Count} campaign(s) eligible for batch calls.");

                foreach (var campaign in campaigns)
                {
                    try
                    {
                        await ProcessCampaignBatchCallAsync(campaign);
                    }
                    catch (Exception ex)
                    {
                        _log.LogError($"[CampaignBatchCall] Error processing campaign #{campaign.CampaignId} '{campaign.CampaignName}': {ex.Message}");
                        await _errorLogger.LogMessage(LogLevelInfo.Exception,
                            new Exception($"CampaignBatchCall failed for campaign #{campaign.CampaignId}", ex));
                    }
                }

                _log.LogInformation("[CampaignBatchCall] Batch done.");
            }
            catch (Exception ex)
            {
                _log.LogError($"[CampaignBatchCall] Job-level exception: {ex.Message}");
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        private async Task ProcessCampaignBatchCallAsync(CampaignBatchCallEligible campaign)
        {
            _log.LogInformation($"[CampaignBatchCall] Processing campaign #{campaign.CampaignId} '{campaign.CampaignName}' (MAG: {campaign.MagName})");

            // Mark as triggered FIRST to prevent duplicate runs if another timer instance fires
            await _eaRepository.MarkCampaignBatchCallTriggeredAsync(campaign.CampaignId);

            var interviewers = await _eaRepository.GetCampaignUnrespondedInterviewersAsync(campaign.CampaignId);
            if (interviewers == null || !interviewers.Any())
            {
                _log.LogInformation($"[CampaignBatchCall] No unresponded interviewers for campaign #{campaign.CampaignId}.");
                return;
            }

            _log.LogInformation($"[CampaignBatchCall] {interviewers.Count} unresponded interviewer(s) for campaign #{campaign.CampaignId}.");

            string callerId = _configuration["PlivoCX:AvailabilityCallerId"]
                           ?? _configuration["PlivoCX:OnboardingCallerId"]
                           ?? _configuration["Plivo:FromNumber"]
                           ?? "+918031136547";

            string source = $"Campaign - {campaign.CampaignName}";
            string initiatedBy = "CampaignBatchCall";

            int batchLogId = 0;
            try
            {
                batchLogId = await _eaRepository.InsertBatchCallLogAsync(
                    campaign.MagName, initiatedBy, interviewers.Count, campaign.CampaignId);
            }
            catch { }

            int succeeded = 0, failed = 0, skipped = 0;

            foreach (var interviewer in interviewers)
            {
                try
                {
                    string phone = interviewer.Phone?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(phone) || phone.Length < 10)
                    {
                        skipped++;
                        continue;
                    }

                    if (phone.Length == 10 && !phone.StartsWith("+")) phone = "+91" + phone;
                    else if (phone.Length > 10 && !phone.StartsWith("+")) phone = "+" + phone;

                    int eaId = await _eaRepository.EnsureEARecordForProfileInterviewerAsync(
                        interviewer.InterviewerProfileId, campaign.MagName, source);

                    if (eaId <= 0)
                    {
                        _log.LogWarning($"[CampaignBatchCall] Could not ensure EA record for profile #{interviewer.InterviewerProfileId}. Skipping.");
                        failed++;
                        continue;
                    }

                    await _eaRepository.UpdatePlivoCallStatusAsync(eaId, "initiated", null,
                        DateTime.UtcNow, null, null, null, null, null, null, null, null, initiatedBy);

                    var triggerRequest = new PlivoCXAvailabilityTriggerRequest
                    {
                        CallerId         = callerId,
                        PhoneNumber      = phone,
                        InterviewerName  = interviewer.InterviewerName ?? "there",
                        InterviewerEmail = interviewer.SenderEmail,
                        MagName          = campaign.MagName,
                        LogId            = eaId.ToString(),
                        TodayDate        = DateTime.Now.ToString("dddd, dd MMMM yyyy")
                    };

                    var response = await _plivoCXService.TriggerAvailabilityCallAsync(triggerRequest);

                    if (!string.IsNullOrEmpty(response?.Error))
                    {
                        await _eaRepository.UpdatePlivoCallStatusAsync(eaId, "trigger-failed", null,
                            null, null, null, response.Error, null, null, null, null, null, null);
                        failed++;
                    }
                    else
                    {
                        await _eaRepository.UpdatePlivoCallStatusAsync(eaId, "triggered", response?.ConversationId,
                            null, null, null, null, null, null, null, null, null, null);
                        await _eaRepository.InsertHistoryAsync(eaId, "CampaignBatchCall", "—",
                            $"Availability call triggered for campaign '{campaign.CampaignName}'. ConvId: {response?.ConversationId ?? "N/A"}", initiatedBy);
                        succeeded++;
                    }

                    await Task.Delay(1200);
                }
                catch (Exception ex)
                {
                    _log.LogError($"[CampaignBatchCall] Error calling interviewer #{interviewer.InterviewerProfileId}: {ex.Message}");
                    failed++;
                }
            }

            if (batchLogId > 0)
            {
                try { await _eaRepository.CompleteBatchCallLogAsync(batchLogId, succeeded, failed, skipped, "Completed"); }
                catch { }
            }

            _log.LogInformation($"[CampaignBatchCall] Campaign #{campaign.CampaignId} done. Succeeded={succeeded}, Failed={failed}, Skipped={skipped}");
        }
    }
}
