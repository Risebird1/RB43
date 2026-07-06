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
    /// ~230s HTTP limit — the old design ran the whole loop in-request, which was torn down mid-run
    /// (HTTP 499) so no calls completed.
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
                dto = JsonConvert.DeserializeObject<CampaignBatchCallRequestDTO>(requestBody);
            }
            catch (Exception ex)
            {
                _log.LogError($"[CampaignBatchCall] Bad request body: {ex.Message}");
                return await Json(req, HttpStatusCode.BadRequest, new { success = false, error = "Invalid request body." });
            }

            if (dto == null || dto.CampaignId <= 0)
                return await Json(req, HttpStatusCode.BadRequest, new { success = false, error = "Missing campaign id." });

            string source = string.IsNullOrWhiteSpace(dto.Source) ? "Campaign" : dto.Source;
            string initiatedBy = string.IsNullOrWhiteSpace(dto.InitiatedBy) ? "Manual" : dto.InitiatedBy;

            // ---- Synchronous prep (fast): resolve campaign, guard, fetch interviewers, open attempt ----
            CampaignBatchCallEligible campaign;
            List<CampaignUnrespondedInterviewer> interviewers;
            (int LogId, int AttemptNumber, string AttemptName) attempt;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IInterviewerEmailAvailabilityRepository>();

                campaign = await repo.GetCampaignForManualBatchCallAsync(dto.CampaignId);
                if (campaign == null)
                    return await Json(req, HttpStatusCode.OK, new { success = false, started = false, message = "Campaign not found." });

                if (campaign.HasRunningAttempt)
                    return await Json(req, HttpStatusCode.OK, new { success = false, started = false, message = "A batch-call attempt for this campaign is already in progress. Please wait for it to finish before starting another." });

                interviewers = await repo.GetCampaignUnrespondedInterviewersAsync(dto.CampaignId);
                if (interviewers == null || interviewers.Count == 0)
                    return await Json(req, HttpStatusCode.OK, new { success = true, started = false, message = "No unresponded interviewers to call." });

                // Mark the campaign so the automated timer job leaves it alone going forward.
                try { await repo.MarkCampaignBatchCallTriggeredAsync(dto.CampaignId); } catch { }

                // Open a named attempt (sequential per campaign). Shows up immediately on the Campaigns
                // page as "Running" so the operator sees the attempt has begun.
                attempt = await repo.StartCampaignBatchCallAttemptAsync(
                    dto.CampaignId, campaign.CampaignName, campaign.MagName, initiatedBy, interviewers.Count);
            }
            catch (Exception ex)
            {
                _log.LogError($"[CampaignBatchCall] Prep failed for campaign #{dto.CampaignId}: {ex.Message}");
                try
                {
                    using var errScope = _scopeFactory.CreateScope();
                    await errScope.ServiceProvider.GetRequiredService<IApplicationErrorLogger>().LogMessage(LogLevelInfo.Exception, ex);
                }
                catch { }
                return await Json(req, HttpStatusCode.InternalServerError, new { success = false, error = "Failed to start the batch call. Please try again." });
            }

            string callerId = _configuration["PlivoCX:AvailabilityCallerId"]
                           ?? _configuration["PlivoCX:OnboardingCallerId"]
                           ?? _configuration["Plivo:FromNumber"]
                           ?? "+918031136547";

            // Hand the actual paced calling to a detached background task with its OWN DI scope (services
            // are scoped, so we cannot reuse the request scope after this method returns). Safe on the
            // Always-On dedicated plan the function runs on.
            _ = Task.Run(() => ProcessCallsAsync(attempt.LogId, attempt.AttemptName, campaign.MagName, source, callerId, interviewers));

            _log.LogInformation($"[CampaignBatchCall] Attempt '{attempt.AttemptName}' (log #{attempt.LogId}) started for campaign " +
                                $"#{dto.CampaignId} '{campaign.CampaignName}' — {interviewers.Count} interviewer(s) queued.");

            return await Json(req, HttpStatusCode.Accepted, new
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

        // Places the paced Plivo calls for an already-opened attempt, logs each outcome, and finalizes
        // the attempt log. Runs detached from the HTTP request; creates its own scope for DB/Plivo work.
        private async Task ProcessCallsAsync(
            int batchCallLogId, string attemptName, string magName, string source, string callerId,
            List<CampaignUnrespondedInterviewer> interviewers)
        {
            var log = _loggerFactory.CreateLogger<CampaignBatchCall>();
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IInterviewerEmailAvailabilityRepository>();
            var plivoCXService = scope.ServiceProvider.GetRequiredService<IPlivoCXService>();
            var errorLogger = scope.ServiceProvider.GetRequiredService<IApplicationErrorLogger>();

            int succeeded = 0, failed = 0, skipped = 0;
            try
            {
                foreach (var interviewer in interviewers)
                {
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
                        }
                        else
                        {
                            if (phone.Length == 10 && !phone.StartsWith("+")) phone = "+91" + phone;
                            else if (phone.Length > 10 && !phone.StartsWith("+")) phone = "+" + phone;

                            // Defer EA record creation: the row is created by the hangup webhook
                            // only if the call confirms availability.
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
                        log.LogError($"[CampaignBatchCall] Error calling interviewer #{interviewer.InterviewerProfileId}: {exCall.Message}");
                    }

                    try
                    {
                        await repo.InsertCampaignBatchCallDetailAsync(
                            batchCallLogId, interviewer.InterviewerProfileId, interviewer.InterviewerName,
                            interviewer.SenderEmail, phone, outcome, error, conversationId);
                    }
                    catch (Exception exLog)
                    {
                        log.LogError($"[CampaignBatchCall] Failed to log detail for interviewer #{interviewer.InterviewerProfileId}: {exLog.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError($"[CampaignBatchCall] Attempt #{batchCallLogId} failed: {ex.Message}");
                try { await errorLogger.LogMessage(LogLevelInfo.Exception, ex); } catch { }
            }
            finally
            {
                try { await repo.CompleteBatchCallLogAsync(batchCallLogId, succeeded, failed, skipped, "Completed"); }
                catch (Exception exComplete)
                {
                    log.LogError($"[CampaignBatchCall] Failed to finalize attempt #{batchCallLogId}: {exComplete.Message}");
                }
            }

            log.LogInformation($"[CampaignBatchCall] Attempt '{attemptName}' done. Succeeded={succeeded}, Failed={failed}, Skipped={skipped}");
        }

        private static async Task<HttpResponseData> Json(HttpRequestData req, HttpStatusCode status, object body)
        {
            var response = req.CreateResponse(status);
            await response.WriteAsJsonAsync(body);
            // WriteAsJsonAsync can reset the status to 200 OK, so re-assert the intended status after.
            response.StatusCode = status;
            return response;
        }
    }
}
