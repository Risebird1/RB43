using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.PlivoWhatsAppModels;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Threading.Tasks;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// Azure Timer Function — sends automated WhatsApp interview scheduling messages.
    /// Runs every 2 minutes between 9 AM and 9 PM IST.
    ///
    /// The function:
    ///   1. Fetches eligible candidates (usp_GetEligibleCandidatesForPlivoWhatsApp)
    ///   2. Sends a WhatsApp template message with two date slot options
    ///   3. Saves the log and awaits reply (handled by InboundMessage webhook)
    /// </summary>
    public class PlivoWhatsAppScheduleJob
    {
        private readonly IPlivoWhatsAppService    _whatsAppService;
        private readonly IPlivoWhatsAppRepository _repository;
        private readonly IApplicationErrorLogger  _applicationErrorLogger;
        private readonly IPhoneNumberHelper       _phoneNumberHelper;
        private readonly ILogger                  _log;

        // Default interview start time for slot options
        private static readonly TimeSpan DefaultInterviewTime = new TimeSpan(10, 0, 0);

        public PlivoWhatsAppScheduleJob(
            ILoggerFactory           loggerFactory,
            IPlivoWhatsAppService    whatsAppService,
            IPlivoWhatsAppRepository repository,
            IApplicationErrorLogger  applicationErrorLogger,
            IPhoneNumberHelper       phoneNumberHelper)
        {
            _whatsAppService        = whatsAppService;
            _repository             = repository;
            _applicationErrorLogger = applicationErrorLogger;
            _phoneNumberHelper      = phoneNumberHelper;
            _log = loggerFactory.CreateLogger<PlivoWhatsAppScheduleJob>();
        }

        [Function("PlivoWhatsAppScheduleJob")]
        public async Task Run([TimerTrigger("0 */2 * * * *")] TimerInfo myTimer)
        {
            var currentTime = DateTime.Now;
            _log.LogInformation($"[WA-Schedule] Job triggered at: {currentTime}");

            try
            {
                // Only run between 9 AM and 9 PM IST
                if (currentTime.Hour < 9 || currentTime.Hour >= 21)
                {
                    _log.LogInformation($"[WA-Schedule] Skipping — {currentTime:HH:mm} is outside business hours");
                    return;
                }

                var candidates = await _repository.GetEligibleCandidatesForScheduleAsync(2);
                if (candidates == null || !candidates.Any())
                {
                    _log.LogInformation("[WA-Schedule] No eligible candidates");
                    return;
                }

                _log.LogInformation($"[WA-Schedule] Found {candidates.Count} eligible candidate(s)");

                int successCount = 0, failureCount = 0;

                foreach (var candidate in candidates)
                {
                    try
                    {
                        _log.LogInformation($"[WA-Schedule] Processing: {candidate.CandidateName} " +
                            $"(Email: {candidate.CandidateEmail}, IRFId: {candidate.IRFId})");

                        if (!_phoneNumberHelper.TrySanitizeIndianPhoneNumber(
                                candidate.CandidatePhone, out var phoneNumber, out var phoneError))
                        {
                            _log.LogWarning($"[WA-Schedule] Skipping {candidate.CandidateEmail} — invalid phone: {phoneError}");
                            continue;
                        }

                        var tomorrow = DateTime.Now.AddDays(1);
                        var dayAfter = DateTime.Now.AddDays(2);

                        var context = new PlivoWhatsAppTriggerContext
                        {
                            CandidateName     = candidate.CandidateName,
                            CandidatePhone    = phoneNumber,
                            CandidateEmail    = candidate.CandidateEmail,
                            IrfId             = candidate.IRFId,
                            CandidateId       = candidate.CandidateId,
                            JobTitle          = candidate.JobTitle ?? candidate.IRFName,
                            ClientName        = candidate.ClientName,
                            MessagePurpose    = "schedule",
                            TriggeredBy       = "PlivoWhatsAppScheduleTimerFunction",
                            TodayDate         = DateTime.Now.ToString("dddd, dd MMMM yyyy"),
                            TomorrowDateLabel = tomorrow.ToString("dddd, dd MMMM yyyy"),
                            DayAfterDateLabel = dayAfter.ToString("dddd, dd MMMM yyyy"),
                            Option1DateTime   = tomorrow.Date + DefaultInterviewTime,
                            Option2DateTime   = dayAfter.Date  + DefaultInterviewTime
                        };

                        // Send and save (controller's helper method logic inlined here)
                        var log = new PlivoWhatsAppLog
                        {
                            RecipientName  = context.CandidateName,
                            RecipientPhone = context.CandidatePhone,
                            RecipientEmail = context.CandidateEmail,
                            IRFId          = context.IrfId,
                            CandidateId    = context.CandidateId,
                            JobTitle       = context.JobTitle,
                            ClientName     = context.ClientName,
                            MessagePurpose = context.MessagePurpose,
                            Status         = "initiated",
                            MessageStatus  = "initiated",
                            TriggeredBy    = context.TriggeredBy
                        };
                        int logId = await _repository.SaveLogAsync(log);
                        log.Id = logId;

                        context.LogId = logId.ToString();

                        var response = await _whatsAppService.SendScheduleMessageAsync(context);

                        // Debug log — persist raw trigger request + response for diagnostics
                        try
                        {
                            await _repository.SaveDebugLogAsync(new PlivoWhatsAppDebugLog
                            {
                                LogType          = "timer-schedule-request",
                                HttpStatusCode   = response?.HttpStatusCode,
                                PlivoCXId        = response?.ApiId,
                                LogId            = logId,
                                RawPayload       = Newtonsoft.Json.JsonConvert.SerializeObject(context),
                                ProcessingStatus = string.IsNullOrEmpty(response?.Error) ? "success" : "failed",
                                ErrorMessage     = response?.Error
                            });
                            await _repository.SaveDebugLogAsync(new PlivoWhatsAppDebugLog
                            {
                                LogType          = "timer-schedule-response",
                                HttpStatusCode   = response?.HttpStatusCode,
                                PlivoCXId        = response?.ApiId,
                                LogId            = logId,
                                RawPayload       = response?.RawResponseBody,
                                ProcessingStatus = string.IsNullOrEmpty(response?.Error) ? "success" : "failed",
                                ErrorMessage     = response?.Error
                            });
                        }
                        catch { }

                        if (!string.IsNullOrEmpty(response?.Error))
                        {
                            _log.LogWarning($"[WA-Schedule] Send failed for {candidate.CandidateEmail}: {response.Error}");
                            log.Status             = "trigger-failed";
                            log.MessageStatus      = "trigger-failed";
                            log.BookingApiResponse = response.Error;
                            await _repository.UpdateLogAsync(log);
                            failureCount++;
                            continue;
                        }

                        log.PlivoCXConversationId = response?.ConversationId;
                        log.PlivoCXRequestId      = response?.ApiId;
                        log.MessageStatus         = "triggered";
                        log.Status                = "triggered";
                        await _repository.UpdateLogAsync(log);

                        if (candidate.CandidateId > 0)
                            await _repository.IncrementCallAttemptsAsync(candidate.CandidateId);

                        _log.LogInformation($"[WA-Schedule] Triggered for {candidate.CandidateEmail} — ConversationId: {log.PlivoCXConversationId}");
                        successCount++;
                        await Task.Delay(2000);
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _log.LogError($"[WA-Schedule] Failed for {candidate.CandidateEmail}: {ex.Message}");
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                            new Exception($"WhatsApp schedule failed for {candidate.CandidateEmail}", ex));
                    }
                }

                _log.LogInformation($"[WA-Schedule] Job done — Success: {successCount}, Failed: {failureCount}");
            }
            catch (Exception ex)
            {
                _log.LogError($"[WA-Schedule] Job exception: {ex.Message}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
