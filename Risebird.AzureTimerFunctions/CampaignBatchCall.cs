using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.Enum;
using Risebird.Entities.Helpers;
using Risebird.Repository.Interface;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// HTTP-triggered worker that performs a campaign batch call OUTSIDE the main portal. The portal
    /// opens the attempt log row and then fires this function (fire-and-forget) with the interviewer
    /// list. This function paces one Plivo availability call per interviewer, records each call's
    /// outcome in IABatchCallAttemptDetail, and finalizes the attempt (IABatchCallLog) when done.
    ///
    /// Offloading the calling here keeps the portal request instant and frees it from holding the
    /// connection open for the many minutes a large batch takes.
    /// </summary>
    public class CampaignBatchCall
    {
        private readonly IInterviewerEmailAvailabilityRepository _eaRepository;
        private readonly IPlivoCXService _plivoCXService;
        private readonly IApplicationErrorLogger _errorLogger;
        private readonly ILogger _log;

        public CampaignBatchCall(
            ILoggerFactory loggerFactory,
            IInterviewerEmailAvailabilityRepository eaRepository,
            IPlivoCXService plivoCXService,
            IApplicationErrorLogger errorLogger)
        {
            _eaRepository = eaRepository;
            _plivoCXService = plivoCXService;
            _errorLogger = errorLogger;
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
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteAsJsonAsync(new { success = false, error = "Invalid request body." });
                return bad;
            }

            if (dto == null || dto.BatchCallLogId <= 0)
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteAsJsonAsync(new { success = false, error = "Missing batch-call attempt context." });
                return bad;
            }

            // Read the unresponded interviewers directly from the DB (the portal now sends only the
            // lightweight attempt context, not the full list). This keeps the fire-and-forget request
            // body tiny and lets the function work at its own pace.
            System.Collections.Generic.List<CampaignUnrespondedInterviewer> interviewers;
            try
            {
                interviewers = await _eaRepository.GetCampaignUnrespondedInterviewersAsync(dto.CampaignId);
            }
            catch (Exception exLoad)
            {
                _log.LogError($"[CampaignBatchCall] Failed to load interviewers for campaign #{dto.CampaignId}: {exLoad.Message}");
                await _errorLogger.LogMessage(LogLevelInfo.Exception, exLoad);
                try { await _eaRepository.CompleteBatchCallLogAsync(dto.BatchCallLogId, 0, 0, 0, "Failed"); } catch { }
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                await err.WriteAsJsonAsync(new { success = false, error = "Failed to load interviewers." });
                return err;
            }

            _log.LogInformation($"[CampaignBatchCall] Attempt '{dto.AttemptName}' (log #{dto.BatchCallLogId}) for campaign #{dto.CampaignId} " +
                                $"'{dto.CampaignName}' — {interviewers?.Count ?? 0} interviewer(s).");

            int succeeded = 0, failed = 0, skipped = 0;
            try
            {
                if (interviewers != null)
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
                                    CallerId         = dto.CallerId,
                                    PhoneNumber      = phone,
                                    InterviewerName  = interviewer.InterviewerName ?? "there",
                                    InterviewerEmail = interviewer.SenderEmail,
                                    MagName          = dto.MagName,
                                    LogId            = AvailabilityCallToken.Build(interviewer.InterviewerProfileId, dto.MagName, dto.Source),
                                    TodayDate        = DateTime.Now.ToString("dddd, dd MMMM yyyy")
                                };

                                var response = await _plivoCXService.TriggerAvailabilityCallAsync(triggerRequest);

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

                                await Task.Delay(1200);
                            }
                        }
                        catch (Exception exCall)
                        {
                            failed++;
                            outcome = "Failed";
                            error = exCall.Message;
                            _log.LogError($"[CampaignBatchCall] Error calling interviewer #{interviewer.InterviewerProfileId}: {exCall.Message}");
                        }

                        try
                        {
                            await _eaRepository.InsertCampaignBatchCallDetailAsync(
                                dto.BatchCallLogId, interviewer.InterviewerProfileId, interviewer.InterviewerName,
                                interviewer.SenderEmail, phone, outcome, error, conversationId);
                        }
                        catch (Exception exLog)
                        {
                            _log.LogError($"[CampaignBatchCall] Failed to log detail for interviewer #{interviewer.InterviewerProfileId}: {exLog.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError($"[CampaignBatchCall] Attempt #{dto.BatchCallLogId} failed: {ex.Message}");
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            finally
            {
                try { await _eaRepository.CompleteBatchCallLogAsync(dto.BatchCallLogId, succeeded, failed, skipped, "Completed"); }
                catch (Exception exComplete)
                {
                    _log.LogError($"[CampaignBatchCall] Failed to finalize attempt #{dto.BatchCallLogId}: {exComplete.Message}");
                }
            }

            _log.LogInformation($"[CampaignBatchCall] Attempt '{dto.AttemptName}' done. Succeeded={succeeded}, Failed={failed}, Skipped={skipped}");

            var response2 = req.CreateResponse(HttpStatusCode.OK);
            await response2.WriteAsJsonAsync(new { success = true, succeeded, failed, skipped });
            return response2;
        }
    }
}
