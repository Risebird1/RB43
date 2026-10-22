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
    /// Azure Timer Function — triggers automated outbound RESCHEDULING calls via the
    /// Plivo CX reschedule AI agent.
    ///
    /// Runs every 2 minutes between 9 AM and 9 PM IST (same window as the schedule job).
    ///
    /// Eligibility is determined by usp_GetEligibleCandidatesForPlivoCXReschedule:
    ///   • Has a missed / cancelled / redo interview meeting in InterviewMeetings
    ///   • No new confirmed future interview already exists
    ///   • Not cold, not 'uninterested', IRF still active
    ///   • Fewer than 2 completed reschedule calls for this candidate + IRF
    ///
    /// Prerequisites:
    ///   1. Create a second agent flow in cx.plivo.com (the reschedule agent)
    ///   2. Set PlivoCX:RescheduleTriggerEndpointUrl in appsettings.json
    ///   3. Configure the SAME Hangup URL in the reschedule agent's Events tab
    ///      (the HangupCallback endpoint already handles both purposes via CallPurpose)
    /// </summary>
    public class PlivoCXRescheduleJob
    {
        private readonly IConfiguration          _configuration;
        private readonly IPlivoCXService          _plivoCXService;
        private readonly IPlivoCXCallRepository   _repository;
        private readonly IApplicationErrorLogger  _applicationErrorLogger;
        private readonly IPhoneNumberHelper       _phoneNumberHelper;
        private readonly ILogger                  _log;

        public PlivoCXRescheduleJob(
            ILoggerFactory           loggerFactory,
            IConfiguration           configuration,
            IPlivoCXService          plivoCXService,
            IPlivoCXCallRepository   repository,
            IApplicationErrorLogger  applicationErrorLogger,
            IPhoneNumberHelper       phoneNumberHelper)
        {
            _configuration          = configuration;
            _plivoCXService         = plivoCXService;
            _repository             = repository;
            _applicationErrorLogger = applicationErrorLogger;
            _phoneNumberHelper      = phoneNumberHelper;
            _log = loggerFactory.CreateLogger<PlivoCXRescheduleJob>();
        }

        [Function("PlivoCXRescheduleJob")]
        public async Task Run([TimerTrigger("0 */2 * * * *")] TimerInfo myTimer)
        {
            var currentTime = DateTime.Now;
            _log.LogInformation($"[PlivoCX-Reschedule] Job triggered at: {currentTime}");

            try
            {
                // PlivoCX:LegacyTimerJobsEnabled = false hands all calling to PlivoCX campaigns
                // (PlivoCXCampaignScheduler). Defaults to on; campaign candidates are excluded either way.
                if (!_configuration.GetValue("PlivoCX:LegacyTimerJobsEnabled", true))
                {
                    _log.LogInformation("[PlivoCX-Reschedule] Legacy timer job disabled (PlivoCX:LegacyTimerJobsEnabled = false)");
                    return;
                }

                // Only run between 9 AM and 9 PM IST
                if (currentTime.Hour < 9 || currentTime.Hour >= 22)
                {
                    _log.LogInformation($"[PlivoCX-Reschedule] Skipping — {currentTime:HH:mm} is outside business hours (9 AM – 9 PM)");
                    return;
                }

                var candidates = await _repository.GetEligibleCandidatesForRescheduleAsync(4, excludeCampaignCandidates: true);
                if (candidates == null || !candidates.Any())
                {
                    _log.LogInformation("[PlivoCX-Reschedule] No eligible candidates for rescheduling");
                    return;
                }

                _log.LogInformation($"[PlivoCX-Reschedule] Found {candidates.Count} eligible candidate(s) for rescheduling");

                int successCount = 0;
                int failureCount = 0;

                foreach (var candidate in candidates)
                {
                    try
                    {
                        _log.LogInformation($"[PlivoCX-Reschedule] Processing: {candidate.CandidateName} " +
                            $"(Email: {candidate.CandidateEmail}, IRFId: {candidate.IRFId}, " +
                            $"OriginalDate: {candidate.OriginalInterviewDate})");

                        if (!_phoneNumberHelper.TrySanitizeIndianPhoneNumber(
                                candidate.CandidatePhone, out var phoneNumber, out var phoneError))
                        {
                            _log.LogWarning($"[PlivoCX-Reschedule] Skipping {candidate.CandidateEmail} — invalid phone: {phoneError}");
                            continue;
                        }

                        string jobTitle   = candidate.JobTitle ?? candidate.IRFName;
                        string clientName = candidate.ClientName;

                        // 1. Save call log (CallPurpose = "reschedule")
                        var callLog = new PlivoCXCallLog
                        {
                            RecipientName    = candidate.CandidateName,
                            RecipientPhone   = phoneNumber,
                            RecipientEmail   = candidate.CandidateEmail,
                            IRFId            = candidate.IRFId,
                            CandidateId      = candidate.CandidateId,
                            JobTitle         = jobTitle,
                            ClientName       = clientName,
                            CallStatus       = "initiated",
                            InterviewBooked  = false,
                            CallPurpose      = "reschedule",
                            TriggeredBy      = "PlivoCXRescheduleTimerFunction"
                        };
                        int logId = await _repository.SaveCallLogAsync(callLog);
                        callLog.Id = logId;

                        // 2. Trigger the reschedule agent
                        var triggerReq = new PlivoCXTriggerRequest
                        {
                            PhoneNumber           = phoneNumber,
                            CandidateName         = candidate.CandidateName ?? "Candidate",
                            CandidateEmail        = candidate.CandidateEmail,
                            IrfId                 = candidate.IRFId.ToString(),
                            JobTitle              = jobTitle,
                            ClientName            = clientName,
                            LogId                 = logId.ToString(),
                            CallPurpose           = "reschedule",
                            OriginalInterviewDate = candidate.OriginalInterviewDate,
                            TodayDate             = DateTime.Now.ToString("dddd, dd MMMM yyyy"),
                            TomorrowDate          = DateTime.Now.AddDays(1).ToString("dddd, dd MMMM yyyy"),
                            DayAfterTomorrowDate  = DateTime.Now.AddDays(2).ToString("dddd, dd MMMM yyyy")
                        };

                        var triggerResponse = await _plivoCXService.TriggerRescheduleCallAsync(triggerReq);

                        if (!string.IsNullOrEmpty(triggerResponse?.Error))
                        {
                            _log.LogWarning($"[PlivoCX-Reschedule] Trigger failed for {candidate.CandidateEmail}: {triggerResponse.Error}");
                            callLog.CallStatus  = "trigger-failed";
                            callLog.HangupCause = triggerResponse.Error;
                            callLog.UpdatedAt   = DateTime.UtcNow;
                            await _repository.UpdateCallLogAsync(callLog);
                            failureCount++;
                            continue;
                        }

                        // 3. Update log with conversation id
                        callLog.PlivoCXConversationId = triggerResponse?.ConversationId;
                        callLog.PlivoCXRequestId      = triggerResponse?.ApiId;
                        callLog.CallStatus            = "triggered";
                        callLog.UpdatedAt             = DateTime.UtcNow;
                        await _repository.UpdateCallLogAsync(callLog);

                        // 4. Increment attempt counter (shared VapiCallAttempts column)
                        if (candidate.CandidateId > 0)
                            await _repository.IncrementCallAttemptsAsync(candidate.CandidateId);

                        _log.LogInformation($"[PlivoCX-Reschedule] Call triggered for {candidate.CandidateEmail} " +
                            $"— ConversationId: {triggerResponse?.ConversationId}");

                        successCount++;
                        await Task.Delay(2000); // rate-limit between candidates
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _log.LogError($"[PlivoCX-Reschedule] Failed for {candidate.CandidateEmail}: {ex.Message}");
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                            new Exception($"PlivoCX reschedule call failed for {candidate.CandidateEmail} (IRFId: {candidate.IRFId})", ex));
                    }
                }

                _log.LogInformation($"[PlivoCX-Reschedule] Job done — Success: {successCount}, Failed: {failureCount}");
            }
            catch (Exception ex)
            {
                _log.LogError($"[PlivoCX-Reschedule] Job exception: {ex.Message}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
