using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.PlivoModels;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// Azure Timer Function for automated Plivo + OpenAI Realtime interview scheduling calls.
    /// Runs every 2 minutes between 9 AM and 9 PM IST.
    /// Identical cadence and eligibility logic to VapiInterviewSchedulingJob;
    /// uses the shared VapiCallAttempts counter to prevent over-calling.
    /// </summary>
    public class PlivoInterviewSchedulingJob
    {
        private readonly IConfiguration _configuration;
        private readonly IPlivoService _plivoService;
        private readonly IPlivoCallRepository _plivoCallRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IPhoneNumberHelper _phoneNumberHelper;
        private readonly ILogger _log;

        public PlivoInterviewSchedulingJob(
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IPlivoService plivoService,
            IPlivoCallRepository plivoCallRepository,
            IApplicationErrorLogger applicationErrorLogger,
            IPhoneNumberHelper phoneNumberHelper)
        {
            _configuration           = configuration;
            _plivoService            = plivoService;
            _plivoCallRepository     = plivoCallRepository;
            _applicationErrorLogger  = applicationErrorLogger;
            _phoneNumberHelper       = phoneNumberHelper;
            _log = loggerFactory.CreateLogger<PlivoInterviewSchedulingJob>();
        }

        [Function("PlivoInterviewSchedulingJob")]
        public async Task Run([TimerTrigger("0 */2 * * * *")] TimerInfo myTimer)
        {
            var currentTime = DateTime.Now;
            var currentHour = currentTime.Hour;

            _log.LogInformation($"Plivo Interview Scheduling Job triggered at: {currentTime}");

            try
            {
                // Only run between 9 AM and 9 PM IST
                if (currentHour < 9 || currentHour >= 21)
                {
                    _log.LogInformation($"Skipping — {currentTime:HH:mm} is outside business hours (9 AM – 9 PM)");
                    return;
                }

                var eligibleCandidates = await _plivoCallRepository.GetEligibleCandidatesForCallsAsync(1);
                if (eligibleCandidates == null || !eligibleCandidates.Any())
                {
                    _log.LogInformation("No eligible candidates found for calling");
                    return;
                }

                _log.LogInformation($"Found {eligibleCandidates.Count} eligible candidate(s)");

                var baseUrl    = _configuration["HostDetails:baseUrl"];
                var fromNumber = _configuration["Plivo:FromNumber"];

                int successCount = 0;
                int failureCount = 0;

                foreach (var candidate in eligibleCandidates)
                {
                    try
                    {
                        _log.LogInformation($"Processing: {candidate.CandidateName} (Email: {candidate.CandidateEmail}, IRFId: {candidate.IRFId}, Attempts: {candidate.CallAttempts})");

                        if (!_phoneNumberHelper.TrySanitizeIndianPhoneNumber(candidate.CandidatePhone, out var phoneNumber, out var phoneError))
                        {
                            _log.LogWarning($"Skipping {candidate.CandidateEmail} — invalid phone: {phoneError}");
                            continue;
                        }

                        // 1. Save call log first to get the DB id for the answer_url
                        var callLog = new PlivoCallLog
                        {
                            RecipientPhone = phoneNumber,
                            RecipientName  = candidate.CandidateName,
                            RecipientEmail = candidate.CandidateEmail,
                            FromNumber     = fromNumber,
                            IRFId          = candidate.IRFId,
                            RecipientId    = candidate.CandidateId,
                            CallStatus     = "queued",
                            AIModel        = "gpt-4o-realtime-preview",
                            CreatedAt      = DateTime.UtcNow,
                            CreatedBy      = "PlivoTimerFunction"
                        };
                        var logId = await _plivoCallRepository.SaveCallLogAsync(callLog);

                        // 2. Build URLs with the DB id embedded
                        var answerUrl = $"{baseUrl}/PlivoInterviewCall/AnswerXml/{logId}";
                        var hangupUrl = $"{baseUrl}/PlivoInterviewCall/HangupCallback";
                        var ringUrl   = $"{baseUrl}/PlivoInterviewCall/RingCallback";

                        // 3. Fire the call — PlivoService increments VapiCallAttempts on success
                        var callRequest = new PlivoOutboundCallRequest
                        {
                            To           = phoneNumber,
                            From         = fromNumber,
                            AnswerUrl    = answerUrl,
                            AnswerMethod = "GET",
                            HangupUrl    = hangupUrl,
                            RingUrl      = ringUrl,
                            TimeLimit    = 300
                        };

                        var callResponse = await _plivoService.InitiateCallAsync(callRequest, candidate.CandidateId);

                        _log.LogInformation($"Call fired for {candidate.CandidateEmail} — RequestUUID: {callResponse.RequestUUID}");

                        // 4. Persist the RequestUUID
                        callLog.Id                = logId;
                        callLog.PlivoRequestUUID  = callResponse.RequestUUID;
                        callLog.UpdatedAt         = DateTime.UtcNow;
                        await _plivoCallRepository.UpdateCallLogAsync(callLog);

                        successCount++;
                        await Task.Delay(2000); // rate-limit between candidates
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _log.LogError($"Failed to call {candidate.CandidateEmail}: {ex.Message}");
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                            new Exception($"Plivo call failed for {candidate.CandidateEmail} (IRFId: {candidate.IRFId})", ex));
                    }
                }

                _log.LogInformation($"Plivo Scheduling Job done — Success: {successCount}, Failed: {failureCount}");
            }
            catch (Exception ex)
            {
                _log.LogError($"Plivo Scheduling Job exception: {ex.Message}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
