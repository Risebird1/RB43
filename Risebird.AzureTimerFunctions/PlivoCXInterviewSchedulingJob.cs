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
    /// Azure Timer Function — triggers automated outbound interview calls via Plivo CX AI Agent.
    /// Runs every 2 minutes between 9 AM and 9 PM IST.
    ///
    /// Unlike the Programmable Voice timer, this simply POSTs to the Plivo CX trigger endpoint
    /// and lets Plivo CX handle the entire AI conversation (STT + LLM + TTS).
    /// No WebSocket bridge, no audio conversion.
    ///
    /// Prerequisites:
    ///   1. Create your agent flow in cx.plivo.com with an API Request trigger
    ///   2. Set PlivoCX:TriggerEndpointUrl in appsettings.json
    ///   3. Configure the Hangup URL in Plivo CX Agent Config → Events
    /// </summary>
    public class PlivoCXInterviewSchedulingJob
    {
        private readonly IConfiguration          _configuration;
        private readonly IPlivoCXService          _plivoCXService;
        private readonly IPlivoCXCallRepository   _repository;
        private readonly IApplicationErrorLogger  _applicationErrorLogger;
        private readonly IPhoneNumberHelper       _phoneNumberHelper;
        private readonly ILogger                  _log;

        public PlivoCXInterviewSchedulingJob(
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
            _log = loggerFactory.CreateLogger<PlivoCXInterviewSchedulingJob>();
        }

        [Function("PlivoCXInterviewSchedulingJob")]
        public async Task Run([TimerTrigger("0 */2 * * * *")] TimerInfo myTimer)
        {
            var currentTime = DateTime.Now;
            _log.LogInformation($"[PlivoCX] Scheduling job triggered at: {currentTime}");

            try
            {
                // Only run between 9 AM and 9 PM IST
                if (currentTime.Hour < 9 || currentTime.Hour >= 23)
                {
                    _log.LogInformation($"[PlivoCX] Skipping — {currentTime:HH:mm} is outside business hours (9 AM – 9 PM)");
                    return;
                }

                var candidates = await _repository.GetEligibleCandidatesForCallsAsync(1);
                if (candidates == null || !candidates.Any())
                {
                    _log.LogInformation("[PlivoCX] No eligible candidates for calling");
                    return;
                }

                _log.LogInformation($"[PlivoCX] Found {candidates.Count} eligible candidate(s)");

                int successCount = 0;
                int failureCount = 0;

                foreach (var candidate in candidates)
                {
                    try
                    {
                        _log.LogInformation($"[PlivoCX] Processing: {candidate.CandidateName} " +
                            $"(Email: {candidate.CandidateEmail}, IRFId: {candidate.IRFId}, Attempts: {candidate.CallAttempts})");

                        if (!_phoneNumberHelper.TrySanitizeIndianPhoneNumber(
                                candidate.CandidatePhone, out var phoneNumber, out var phoneError))
                        {
                            _log.LogWarning($"[PlivoCX] Skipping {candidate.CandidateEmail} — invalid phone: {phoneError}");
                            continue;
                        }

                        string jobTitle   = candidate.JobTitle ?? candidate.IRFName;
                        string clientName = candidate.ClientName;

                        // 1. Save call log to get a DB Id
                        var callLog = new PlivoCXCallLog
                        {
                            RecipientName  = candidate.CandidateName,
                            RecipientPhone = phoneNumber,
                            RecipientEmail = candidate.CandidateEmail,
                            IRFId          = candidate.IRFId,
                            CandidateId    = candidate.CandidateId,
                            JobTitle       = jobTitle,
                            ClientName     = clientName,
                            CallStatus     = "initiated",
                            InterviewBooked = false,
                            TriggeredBy    = "PlivoCXTimerFunction"
                        };
                        int logId = await _repository.SaveCallLogAsync(callLog);
                        callLog.Id = logId;

                        // 2. Trigger the Plivo CX call
                        var triggerReq = new PlivoCXTriggerRequest
                        {
                            PhoneNumber          = phoneNumber,
                            CandidateName        = candidate.CandidateName ?? "Candidate",
                            CandidateEmail       = candidate.CandidateEmail,
                            IrfId                = candidate.IRFId.ToString(),
                            JobTitle             = jobTitle,
                            ClientName           = clientName,
                            LogId                = logId.ToString(),
                            TodayDate            = DateTime.Now.ToString("dddd, dd MMMM yyyy"),
                            TomorrowDate         = DateTime.Now.AddDays(1).ToString("dddd, dd MMMM yyyy"),
                            DayAfterTomorrowDate = DateTime.Now.AddDays(2).ToString("dddd, dd MMMM yyyy")
                        };

                        var triggerResponse = await _plivoCXService.TriggerOutboundCallAsync(triggerReq);

                        if (!string.IsNullOrEmpty(triggerResponse?.Error))
                        {
                            _log.LogWarning($"[PlivoCX] Trigger failed for {candidate.CandidateEmail}: {triggerResponse.Error}");
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

                        // 4. Increment the shared VapiCallAttempts counter
                        if (candidate.CandidateId > 0)
                            await _repository.IncrementCallAttemptsAsync(candidate.CandidateId);

                        _log.LogInformation($"[PlivoCX] Call triggered for {candidate.CandidateEmail} " +
                            $"— ConversationId: {triggerResponse?.ConversationId}");

                        successCount++;
                        await Task.Delay(2000); // rate-limit between candidates
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _log.LogError($"[PlivoCX] Failed for {candidate.CandidateEmail}: {ex.Message}");
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                            new Exception($"PlivoCX call failed for {candidate.CandidateEmail} (IRFId: {candidate.IRFId})", ex));
                    }
                }

                _log.LogInformation($"[PlivoCX] Job done — Success: {successCount}, Failed: {failureCount}");
            }
            catch (Exception ex)
            {
                _log.LogError($"[PlivoCX] Scheduling job exception: {ex.Message}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
