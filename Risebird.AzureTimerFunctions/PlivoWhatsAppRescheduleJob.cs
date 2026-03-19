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
    /// Azure Timer Function — sends automated WhatsApp interview RESCHEDULING messages.
    /// Runs every 2 minutes between 9 AM and 9 PM IST.
    ///
    /// Mirrors PlivoWhatsAppScheduleJob but for rescheduling:
    ///   • Uses usp_GetEligibleCandidatesForPlivoWhatsAppReschedule
    ///   • Sends the reschedule template (includes OriginalInterviewDate variable)
    ///   • MessagePurpose = "reschedule"
    /// </summary>
    public class PlivoWhatsAppRescheduleJob
    {
        private readonly IPlivoWhatsAppService    _whatsAppService;
        private readonly IPlivoWhatsAppRepository _repository;
        private readonly IApplicationErrorLogger  _applicationErrorLogger;
        private readonly IPhoneNumberHelper       _phoneNumberHelper;
        private readonly ILogger                  _log;

        private static readonly TimeSpan DefaultInterviewTime = new TimeSpan(10, 0, 0);

        public PlivoWhatsAppRescheduleJob(
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
            _log = loggerFactory.CreateLogger<PlivoWhatsAppRescheduleJob>();
        }

        [Function("PlivoWhatsAppRescheduleJob")]
        public async Task Run([TimerTrigger("0 */2 * * * *")] TimerInfo myTimer)
        {
            var currentTime = DateTime.Now;
            _log.LogInformation($"[WA-Reschedule] Job triggered at: {currentTime}");

            try
            {
                if (currentTime.Hour < 9 || currentTime.Hour >= 21)
                {
                    _log.LogInformation($"[WA-Reschedule] Skipping — {currentTime:HH:mm} is outside business hours");
                    return;
                }

                var candidates = await _repository.GetEligibleCandidatesForRescheduleAsync(2);
                if (candidates == null || !candidates.Any())
                {
                    _log.LogInformation("[WA-Reschedule] No eligible candidates");
                    return;
                }

                _log.LogInformation($"[WA-Reschedule] Found {candidates.Count} eligible candidate(s)");

                int successCount = 0, failureCount = 0;

                foreach (var candidate in candidates)
                {
                    try
                    {
                        _log.LogInformation($"[WA-Reschedule] Processing: {candidate.CandidateName} " +
                            $"(Email: {candidate.CandidateEmail}, IRFId: {candidate.IRFId}, " +
                            $"OriginalDate: {candidate.OriginalInterviewDate})");

                        if (!_phoneNumberHelper.TrySanitizeIndianPhoneNumber(
                                candidate.CandidatePhone, out var phoneNumber, out var phoneError))
                        {
                            _log.LogWarning($"[WA-Reschedule] Skipping {candidate.CandidateEmail} — invalid phone: {phoneError}");
                            continue;
                        }

                        var tomorrow = DateTime.Now.AddDays(1);
                        var dayAfter = DateTime.Now.AddDays(2);

                        var context = new PlivoWhatsAppTriggerContext
                        {
                            CandidateName         = candidate.CandidateName,
                            CandidatePhone        = phoneNumber,
                            CandidateEmail        = candidate.CandidateEmail,
                            IrfId                 = candidate.IRFId,
                            CandidateId           = candidate.CandidateId,
                            JobTitle              = candidate.JobTitle ?? candidate.IRFName,
                            ClientName            = candidate.ClientName,
                            MessagePurpose        = "reschedule",
                            OriginalInterviewDate = candidate.OriginalInterviewDate,
                            TriggeredBy           = "PlivoWhatsAppRescheduleTimerFunction",
                            TodayDate             = DateTime.Now.ToString("dddd, dd MMMM yyyy"),
                            TomorrowDateLabel     = tomorrow.ToString("dddd, dd MMMM yyyy"),
                            DayAfterDateLabel     = dayAfter.ToString("dddd, dd MMMM yyyy"),
                            Option1DateTime       = tomorrow.Date + DefaultInterviewTime,
                            Option2DateTime       = dayAfter.Date  + DefaultInterviewTime
                        };

                        var log = new PlivoWhatsAppLog
                        {
                            RecipientName         = context.CandidateName,
                            RecipientPhone        = context.CandidatePhone,
                            RecipientEmail        = context.CandidateEmail,
                            IRFId                 = context.IrfId,
                            CandidateId           = context.CandidateId,
                            JobTitle              = context.JobTitle,
                            ClientName            = context.ClientName,
                            MessagePurpose        = "reschedule",
                            OriginalInterviewDate = context.OriginalInterviewDate,
                            Status                = "initiated",
                            MessageStatus         = "initiated",
                            TriggeredBy           = context.TriggeredBy
                        };
                        int logId = await _repository.SaveLogAsync(log);
                        log.Id = logId;

                        context.LogId = logId.ToString();

                        var response = await _whatsAppService.SendRescheduleMessageAsync(context);

                        // Debug log — persist raw trigger request + response for diagnostics
                        try
                        {
                            await _repository.SaveDebugLogAsync(new PlivoWhatsAppDebugLog
                            {
                                LogType          = "timer-reschedule-request",
                                HttpStatusCode   = response?.HttpStatusCode,
                                PlivoCXId        = response?.ApiId,
                                LogId            = logId,
                                RawPayload       = Newtonsoft.Json.JsonConvert.SerializeObject(context),
                                ProcessingStatus = string.IsNullOrEmpty(response?.Error) ? "success" : "failed",
                                ErrorMessage     = response?.Error
                            });
                            await _repository.SaveDebugLogAsync(new PlivoWhatsAppDebugLog
                            {
                                LogType          = "timer-reschedule-response",
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
                            _log.LogWarning($"[WA-Reschedule] Send failed for {candidate.CandidateEmail}: {response.Error}");
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

                        _log.LogInformation($"[WA-Reschedule] Triggered for {candidate.CandidateEmail} — ConversationId: {log.PlivoCXConversationId}");
                        successCount++;
                        await Task.Delay(2000);
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _log.LogError($"[WA-Reschedule] Failed for {candidate.CandidateEmail}: {ex.Message}");
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                            new Exception($"WhatsApp reschedule failed for {candidate.CandidateEmail}", ex));
                    }
                }

                _log.LogInformation($"[WA-Reschedule] Job done — Success: {successCount}, Failed: {failureCount}");
            }
            catch (Exception ex)
            {
                _log.LogError($"[WA-Reschedule] Job exception: {ex.Message}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
