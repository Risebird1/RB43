using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.Enum;
using Risebird.Entities.Helpers;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// HTTP-triggered worker that performs a campaign batch call OUTSIDE the main portal. The portal
    /// only fires a lightweight trigger (campaign id + who started it); THIS function owns everything:
    /// it resolves the campaign, guards against an attempt that is already running, reads the
    /// unresponded interviewers from the DB, opens the attempt log, and then places one paced Plivo
    /// availability call per interviewer, recording each outcome in IABatchCallAttemptDetail and
    /// finalizing the attempt (IABatchCallLog) when done.
    ///
    /// The HTTP call returns as soon as the attempt is opened; the actual multi-minute calling loop runs
    /// in a detached background task (with its own DI scope). This keeps the request well under Azure's
    /// ~230s HTTP limit.
    /// </summary>
    public class CampaignBatchCall
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _log;

        private const int InterCallDelayMs = 1200;

        public CampaignBatchCall(
            ILoggerFactory loggerFactory,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _loggerFactory = loggerFactory;
            _log = loggerFactory.CreateLogger<CampaignBatchCall>();
        }

        [Function("CampaignBatchCall")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req)
        {
            CampaignBatchCallRequestDTO dto = null;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _log.LogInformation("[CampaignBatchCall] Received request body: {BodyLength} chars", requestBody?.Length ?? 0);
                dto = JsonConvert.DeserializeObject<CampaignBatchCallRequestDTO>(requestBody);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[CampaignBatchCall] Failed to deserialize request body");
                return await JsonResponse(req, HttpStatusCode.BadRequest,
                    new { success = false, error = $"Invalid request body: {ex.Message}" });
            }

            if (dto == null || dto.CampaignId <= 0)
            {
                _log.LogWarning("[CampaignBatchCall] Bad request — CampaignId is missing or <= 0. DTO null: {IsNull}", dto == null);
                return await JsonResponse(req, HttpStatusCode.BadRequest,
                    new { success = false, error = "Missing campaign id." });
            }

            string source = string.IsNullOrWhiteSpace(dto.Source) ? "Campaign" : dto.Source;
            string initiatedBy = string.IsNullOrWhiteSpace(dto.InitiatedBy) ? "Manual" : dto.InitiatedBy;

            _log.LogInformation("[CampaignBatchCall] Processing campaign #{CampaignId}, initiatedBy={InitiatedBy}, source={Source}",
                dto.CampaignId, initiatedBy, source);

            CampaignBatchCallEligible campaign;
            List<CampaignUnrespondedInterviewer> interviewers;
            (int LogId, int AttemptNumber, string AttemptName) attempt;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IInterviewerEmailAvailabilityRepository>();

                _log.LogInformation("[CampaignBatchCall] Resolving campaign #{CampaignId} from DB…", dto.CampaignId);
                campaign = await repo.GetCampaignForManualBatchCallAsync(dto.CampaignId);
                if (campaign == null)
                {
                    _log.LogWarning("[CampaignBatchCall] Campaign #{CampaignId} not found in DB", dto.CampaignId);
                    return await JsonResponse(req, HttpStatusCode.OK,
                        new { success = false, started = false, message = "Campaign not found." });
                }

                if (campaign.HasRunningAttempt)
                {
                    _log.LogWarning("[CampaignBatchCall] Campaign #{CampaignId} already has a running attempt", dto.CampaignId);
                    return await JsonResponse(req, HttpStatusCode.OK,
                        new { success = false, started = false, message = "A batch-call attempt for this campaign is already in progress. Please wait for it to finish before starting another." });
                }

                _log.LogInformation("[CampaignBatchCall] Fetching unresponded interviewers for campaign #{CampaignId}…", dto.CampaignId);
                interviewers = await repo.GetCampaignUnrespondedInterviewersAsync(dto.CampaignId);
                if (interviewers == null || interviewers.Count == 0)
                {
                    _log.LogInformation("[CampaignBatchCall] No unresponded interviewers for campaign #{CampaignId}", dto.CampaignId);
                    return await JsonResponse(req, HttpStatusCode.OK,
                        new { success = true, started = false, message = "No unresponded interviewers to call." });
                }

                try { await repo.MarkCampaignBatchCallTriggeredAsync(dto.CampaignId); }
                catch (Exception exMark)
                {
                    _log.LogWarning(exMark, "[CampaignBatchCall] Non-critical: failed to mark campaign #{CampaignId} as triggered", dto.CampaignId);
                }

                _log.LogInformation("[CampaignBatchCall] Starting attempt for campaign #{CampaignId} '{CampaignName}', {Count} interviewer(s)…",
                    dto.CampaignId, campaign.CampaignName, interviewers.Count);
                attempt = await repo.StartCampaignBatchCallAttemptAsync(
                    dto.CampaignId, campaign.CampaignName, campaign.MagName, initiatedBy, interviewers.Count);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[CampaignBatchCall] Prep phase failed for campaign #{CampaignId}. Exception: {ExType}: {ExMessage}\n{StackTrace}",
                    dto.CampaignId, ex.GetType().Name, ex.Message, ex.StackTrace);
                try
                {
                    using var errScope = _scopeFactory.CreateScope();
                    await errScope.ServiceProvider.GetRequiredService<IApplicationErrorLogger>()
                        .LogMessage(LogLevelInfo.Exception, ex);
                }
                catch (Exception exLog)
                {
                    _log.LogError(exLog, "[CampaignBatchCall] Additionally failed to write to ApplicationErrorLog");
                }
                return await JsonResponse(req, HttpStatusCode.InternalServerError,
                    new { success = false, error = $"Failed to start the batch call: {ex.Message}" });
            }

            string callerId = _configuration["PlivoCX:AvailabilityCallerId"]
                           ?? _configuration["PlivoCX:OnboardingCallerId"]
                           ?? _configuration["Plivo:FromNumber"]
                           ?? "+918031136547";

            _log.LogInformation("[CampaignBatchCall] Attempt '{AttemptName}' (log #{LogId}) opened for campaign #{CampaignId} '{CampaignName}' — {Count} interviewer(s). Handing off to background task.",
                attempt.AttemptName, attempt.LogId, dto.CampaignId, campaign.CampaignName, interviewers.Count);

            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessCallsAsync(attempt.LogId, attempt.AttemptName, campaign.MagName, source, callerId, interviewers);
                }
                catch (Exception exBg)
                {
                    var bgLog = _loggerFactory.CreateLogger<CampaignBatchCall>();
                    bgLog.LogError(exBg, "[CampaignBatchCall][BG-FATAL] Unhandled exception in background task for attempt #{LogId} '{AttemptName}'. " +
                        "Exception: {ExType}: {ExMessage}\n{StackTrace}",
                        attempt.LogId, attempt.AttemptName, exBg.GetType().Name, exBg.Message, exBg.StackTrace);

                    try
                    {
                        using var errScope = _scopeFactory.CreateScope();
                        await errScope.ServiceProvider.GetRequiredService<IApplicationErrorLogger>()
                            .LogMessage(LogLevelInfo.Exception,
                                new Exception($"[CampaignBatchCall] BG-FATAL for attempt #{attempt.LogId} '{attempt.AttemptName}'", exBg));
                    }
                    catch { }

                    try
                    {
                        using var cleanupScope = _scopeFactory.CreateScope();
                        var cleanupRepo = cleanupScope.ServiceProvider.GetRequiredService<IInterviewerEmailAvailabilityRepository>();
                        await cleanupRepo.CompleteBatchCallLogAsync(attempt.LogId, 0, 0, 0, "Failed");
                    }
                    catch { }
                }
            });

            return await JsonResponse(req, HttpStatusCode.Accepted, new
            {
                success = true,
                started = true,
                queued = interviewers.Count,
                attemptName = attempt.AttemptName,
                attemptNumber = attempt.AttemptNumber,
                message = $"Batch call attempt #{attempt.AttemptNumber} started for {interviewers.Count} unresponded interviewer(s). " +
                          "Calls are being placed by a background service — refresh this page in a few minutes to see the outcome."
            });
        }

        private async Task ProcessCallsAsync(
            int batchCallLogId, string attemptName, string magName, string source, string callerId,
            List<CampaignUnrespondedInterviewer> interviewers)
        {
            var log = _loggerFactory.CreateLogger<CampaignBatchCall>();

            log.LogInformation("[CampaignBatchCall][BG] Attempt #{LogId} '{AttemptName}' — initializing DI scope and resolving services…",
                batchCallLogId, attemptName);

            IServiceScope scope = null;
            IInterviewerEmailAvailabilityRepository repo = null;
            IPlivoCXService plivoCXService = null;
            IApplicationErrorLogger errorLogger = null;

            try
            {
                scope = _scopeFactory.CreateScope();
                repo = scope.ServiceProvider.GetRequiredService<IInterviewerEmailAvailabilityRepository>();
                plivoCXService = scope.ServiceProvider.GetRequiredService<IPlivoCXService>();
                errorLogger = scope.ServiceProvider.GetRequiredService<IApplicationErrorLogger>();
            }
            catch (Exception exDi)
            {
                log.LogError(exDi, "[CampaignBatchCall][BG] DI resolution failed for attempt #{LogId}. " +
                    "Exception: {ExType}: {ExMessage}\n{StackTrace}",
                    batchCallLogId, exDi.GetType().Name, exDi.Message, exDi.StackTrace);
                scope?.Dispose();
                throw;
            }

            int succeeded = 0, failed = 0, skipped = 0;
            bool faulted = false;

            try
            {
                log.LogInformation("[CampaignBatchCall][BG] Attempt #{LogId} — beginning calls for {Count} interviewer(s). CallerId={CallerId}, Source={Source}",
                    batchCallLogId, interviewers.Count, callerId, source);

                for (int i = 0; i < interviewers.Count; i++)
                {
                    var interviewer = interviewers[i];
                    string outcome = "Skipped";
                    string error = null;
                    string conversationId = null;
                    string phone = interviewer.Phone?.Trim() ?? "";

                    try
                    {
                        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 10)
                        {
                            skipped++;
                            error = "Missing or invalid phone number.";
                            log.LogWarning("[CampaignBatchCall][BG] Attempt #{LogId} — skipping interviewer #{ProfileId} ({Name}): {Reason}",
                                batchCallLogId, interviewer.InterviewerProfileId, interviewer.InterviewerName, error);
                        }
                        else
                        {
                            if (phone.Length == 10 && !phone.StartsWith("+")) phone = "+91" + phone;
                            else if (phone.Length > 10 && !phone.StartsWith("+")) phone = "+" + phone;

                            var triggerRequest = new PlivoCXAvailabilityTriggerRequest
                            {
                                CallerId         = callerId,
                                PhoneNumber      = phone,
                                InterviewerName  = interviewer.InterviewerName ?? "there",
                                InterviewerEmail = interviewer.SenderEmail,
                                MagName          = magName,
                                LogId            = AvailabilityCallToken.Build(interviewer.InterviewerProfileId, magName, source),
                                TodayDate        = DateTime.Now.ToString("dddd, dd MMMM yyyy")
                            };

                            var response = await plivoCXService.TriggerAvailabilityCallAsync(triggerRequest);

                            if (!string.IsNullOrEmpty(response?.Error))
                            {
                                failed++;
                                outcome = "Failed";
                                error = response.Error;
                                log.LogWarning("[CampaignBatchCall][BG] Attempt #{LogId} — Plivo returned error for interviewer #{ProfileId} ({Name}, {Phone}): {Error}",
                                    batchCallLogId, interviewer.InterviewerProfileId, interviewer.InterviewerName, phone, error);
                            }
                            else
                            {
                                succeeded++;
                                outcome = "Succeeded";
                                conversationId = response?.ConversationId;
                            }

                            await Task.Delay(InterCallDelayMs);
                        }
                    }
                    catch (Exception exCall)
                    {
                        failed++;
                        outcome = "Failed";
                        error = exCall.Message;
                        log.LogError(exCall, "[CampaignBatchCall][BG] Attempt #{LogId} — exception calling interviewer #{ProfileId} ({Name}, {Phone}). " +
                            "Exception: {ExType}: {ExMessage}",
                            batchCallLogId, interviewer.InterviewerProfileId, interviewer.InterviewerName, phone,
                            exCall.GetType().Name, exCall.Message);
                    }

                    try
                    {
                        await repo.InsertCampaignBatchCallDetailAsync(
                            batchCallLogId, interviewer.InterviewerProfileId, interviewer.InterviewerName,
                            interviewer.SenderEmail, phone, outcome, error, conversationId);
                    }
                    catch (Exception exDetail)
                    {
                        log.LogError(exDetail, "[CampaignBatchCall][BG] Attempt #{LogId} — failed to persist call detail for interviewer #{ProfileId}. " +
                            "Exception: {ExType}: {ExMessage}",
                            batchCallLogId, interviewer.InterviewerProfileId, exDetail.GetType().Name, exDetail.Message);
                    }

                    if ((i + 1) % 25 == 0)
                    {
                        log.LogInformation("[CampaignBatchCall][BG] Attempt #{LogId} — progress: {Done}/{Total} (succeeded={S}, failed={F}, skipped={K})",
                            batchCallLogId, i + 1, interviewers.Count, succeeded, failed, skipped);
                    }
                }
            }
            catch (Exception ex)
            {
                faulted = true;
                log.LogError(ex, "[CampaignBatchCall][BG] Attempt #{LogId} '{AttemptName}' — loop-level exception after processing {Done} of {Total} interviewers. " +
                    "Exception: {ExType}: {ExMessage}\n{StackTrace}",
                    batchCallLogId, attemptName, succeeded + failed + skipped, interviewers.Count,
                    ex.GetType().Name, ex.Message, ex.StackTrace);
                try { await errorLogger.LogMessage(LogLevelInfo.Exception, ex); } catch { }
            }
            finally
            {
                string finalStatus = faulted ? "Failed" : "Completed";
                try
                {
                    await repo.CompleteBatchCallLogAsync(batchCallLogId, succeeded, failed, skipped, finalStatus);
                    log.LogInformation("[CampaignBatchCall][BG] Attempt #{LogId} '{AttemptName}' finalized as '{Status}'. Succeeded={S}, Failed={F}, Skipped={K}",
                        batchCallLogId, attemptName, finalStatus, succeeded, failed, skipped);
                }
                catch (Exception exComplete)
                {
                    log.LogError(exComplete, "[CampaignBatchCall][BG] Failed to finalize attempt #{LogId}. Exception: {ExType}: {ExMessage}",
                        batchCallLogId, exComplete.GetType().Name, exComplete.Message);
                }

                try { scope?.Dispose(); } catch { }
            }
        }

        private static async Task<HttpResponseData> JsonResponse(HttpRequestData req, HttpStatusCode status, object body)
        {
            var response = req.CreateResponse(status);
            await response.WriteAsJsonAsync(body);
            response.StatusCode = status;
            return response;
        }
    }
}
