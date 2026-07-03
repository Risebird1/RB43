using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// PRIORITY intent-collection push — a parallel, single-purpose complement to
    /// ProspectOnboardingAutomationJob.
    ///
    /// Operators flag up to two MAGs per day as "priority" on the Naukri Onboarding dashboard
    /// when demand for interviewers in those MAGs is unusually high. This job runs every few
    /// minutes between 8 AM and 10 PM IST and does ONE thing: it phones (via Plivo CX) the
    /// prospects mapped to those priority MAGs that are still stuck at the Interest Collection
    /// stage (intent not yet captured), so their intent is collected as fast as possible.
    ///
    /// Unlike the main automation job, it deliberately IGNORES the normal anti-spam caps
    /// (5 calls total / 2 per day). The only throttle is a short between-call cooldown so a
    /// prospect isn't redialled while a just-triggered call is still ringing. It does NOT parse
    /// resumes, send emails, verify employment, run AI fitment, or onboard — those remain the
    /// responsibility of the main pipeline. Priority flags reset daily (handled in SQL).
    /// </summary>
    public class PriorityIntentCallJob
    {
        private readonly IConfiguration             _configuration;
        private readonly INaukriOnboardingRepository _repository;
        private readonly IPlivoCXService            _plivoCXService;
        private readonly IApplicationErrorLogger     _errorLogger;
        private readonly ILogger                     _log;

        // Aggressive defaults: cover as much of the pool as fast as possible. Both priority MAGs are
        // treated equally — the eligibility SP interleaves all prospects strictly by who was called
        // least recently, so the whole pool is worked through once before anyone is re-attempted.
        private const int DefaultBatchSize       = 15;
        private const int DefaultCooldownMinutes = 5;

        // Delay between successive Plivo triggers within a round (throttles the trigger endpoint only).
        private const int InterCallDelayMs = 800;

        public PriorityIntentCallJob(
            ILoggerFactory              loggerFactory,
            IConfiguration              configuration,
            INaukriOnboardingRepository repository,
            IPlivoCXService             plivoCXService,
            IApplicationErrorLogger     errorLogger)
        {
            _configuration  = configuration;
            _repository     = repository;
            _plivoCXService = plivoCXService;
            _errorLogger    = errorLogger;
            _log = loggerFactory.CreateLogger<PriorityIntentCallJob>();
        }

        [Function("PriorityIntentCallJob")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            var ist = DateTime.UtcNow.AddHours(5).AddMinutes(30);
            _log.LogInformation($"[PriorityIntent] Triggered at IST {ist:yyyy-MM-dd HH:mm:ss}.");

            // Operating window: 8 AM – 10 PM IST.
            if (ist.Hour < 8 || ist.Hour >= 22)
            {
                _log.LogInformation("[PriorityIntent] Outside 8 AM–10 PM IST window. Skipping.");
                return;
            }

            try
            {
                var priorityMags = await _repository.GetPriorityMagsAsync();
                if (priorityMags == null || !priorityMags.Any())
                {
                    _log.LogInformation("[PriorityIntent] No priority MAGs set for today. Nothing to do.");
                    return;
                }

                _log.LogInformation($"[PriorityIntent] {priorityMags.Count} priority MAG(s): {string.Join(", ", priorityMags.Select(m => m.MagName))}.");

                int batchSize = GetIntSetting("PriorityIntentCall:BatchSize", DefaultBatchSize);
                int cooldown  = GetIntSetting("PriorityIntentCall:CooldownMinutes", DefaultCooldownMinutes);

                var prospects = await _repository.GetPriorityIntentCallProspectsAsync(batchSize, cooldown);
                if (prospects == null || !prospects.Any())
                {
                    _log.LogInformation("[PriorityIntent] No prospects pending intent collection for priority MAGs (or all in cooldown).");
                    return;
                }

                _log.LogInformation($"[PriorityIntent] Calling {prospects.Count} prospect(s) for intent collection.");

                foreach (var p in prospects)
                {
                    try
                    {
                        await TriggerPriorityCallAsync(p);
                    }
                    catch (Exception ex)
                    {
                        _log.LogError($"[PriorityIntent] Unhandled error for #{p.Id} ({p.CandidateName}): {ex.Message}");
                        await _errorLogger.LogMessage(LogLevelInfo.Exception,
                            new Exception($"PriorityIntent failed for prospect #{p.Id}", ex));
                    }

                    await Task.Delay(InterCallDelayMs);
                }

                _log.LogInformation("[PriorityIntent] Batch done.");
            }
            catch (Exception ex)
            {
                _log.LogError($"[PriorityIntent] Fatal: {ex.Message}");
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        private async Task TriggerPriorityCallAsync(NaukriProspectiveInterviewerVM p)
        {
            string phone = p.ParsedPhone;
            if (string.IsNullOrWhiteSpace(phone))
            {
                _log.LogWarning($"[PriorityIntent] #{p.Id} — no phone number.");
                return;
            }

            if (phone.Length == 10 && !phone.StartsWith("+"))
                phone = "+91" + phone;
            else if (phone.Length > 10 && !phone.StartsWith("+"))
                phone = "+" + phone;

            // Mark in-flight + count the priority attempt up-front so a redial in the next tick
            // is blocked by both the call-status guard and the cooldown even if the call is slow.
            await _repository.UpdatePlivoCallStatusAsync(p.Id, "initiated", null,
                DateTime.UtcNow, null, null, null, null, null, null, "PriorityIntentJob");
            await _repository.RecordPriorityCallAttemptAsync(p.Id);

            var callerId = _configuration["PlivoCX:OnboardingCallerId"]
                        ?? _configuration["Plivo:FromNumber"]
                        ?? "+918031136547";

            var request = new PlivoCXOnboardingTriggerRequest
            {
                CallerId        = callerId,
                PhoneNumber     = phone,
                ProspectName    = p.CandidateName ?? "there",
                ProspectEmail   = p.ParsedEmail,
                Specialization  = p.MagName ?? p.KeySkills ?? "Technical",
                ExperienceYears = p.ExperienceYears,
                LogId           = p.Id.ToString(),
                TodayDate       = DateTime.Now.ToString("dddd, dd MMMM yyyy")
            };

            var response = await _plivoCXService.TriggerOnboardingCallAsync(request);

            if (!string.IsNullOrEmpty(response?.Error))
            {
                _log.LogWarning($"[PriorityIntent] #{p.Id} — trigger failed: {response.Error}");
                await _repository.UpdatePlivoCallStatusAsync(p.Id, "trigger-failed", null,
                    null, null, null, response.Error, null, null, null, null);
                await _repository.InsertHistoryAsync(p.Id, "PriorityCallNote", "—",
                    $"Priority intent call trigger failed: {response.Error}", "PriorityIntentJob");
                return;
            }

            await _repository.UpdatePlivoCallStatusAsync(p.Id, "triggered", response?.ConversationId,
                null, null, null, null, null, null, null, null);
            await _repository.InsertHistoryAsync(p.Id, "PriorityCallTriggered", "—",
                $"Priority intent-collection call triggered to {p.ParsedPhone} (MAG: {p.MagName}). ConvId: {response?.ConversationId}", "PriorityIntentJob");

            _log.LogInformation($"[PriorityIntent] #{p.Id} — call triggered. ConvId: {response?.ConversationId}");
        }

        private int GetIntSetting(string key, int fallback)
        {
            var raw = _configuration[key];
            return int.TryParse(raw, out var v) && v > 0 ? v : fallback;
        }
    }
}
