using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.PlivoWhatsAppModels;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using Risebird.Web.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using WaCp = Risebird.Entities.Data.PlivoWhatsAppModels.WaCheckpointNames;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// Controller for Plivo CX WhatsApp AI interview scheduling/rescheduling.
    ///
    /// Architecture (identical to PlivoCXInterviewCallController, different channel):
    ///
    ///   1. InitiateScheduleMessage / InitiateRescheduleMessage
    ///        → saves PlivoWhatsAppLog row → gets logId
    ///        → POSTs to Plivo CX WhatsApp trigger (via IPlivoWhatsAppService) with candidate vars + logId
    ///
    ///   2. Plivo CX handles the full WhatsApp conversation:
    ///        • Sends initial WhatsApp message to the candidate
    ///        • AI Conversation node drives the scheduling dialogue over WhatsApp
    ///        • Extracts variables: interview_date, interview_time, candidate_confirmed, candidate_disposition
    ///
    ///   3. ConversationEndCallback (Plivo CX → our server, configured as "Hangup URL" in Events tab)
    ///        → receives all extracted variables + transcript
    ///        → if candidate_confirmed=true → calls the Schedule API
    ///        → updates PlivoWhatsAppLog
    ///
    /// Configure in cx.plivo.com → AI Studio → Your WhatsApp Flow → Agent Config → Events:
    ///   Hangup URL: https://your-domain.com/PlivoWhatsAppInterview/ConversationEndCallback
    /// </summary>
    [Authorize]
    public class PlivoWhatsAppInterviewController : Controller
    {
        private readonly IConfiguration            _configuration;
        private readonly IPlivoWhatsAppService     _whatsAppService;
        private readonly IPlivoWhatsAppRepository  _repository;
        private readonly IApplicationErrorLogger   _logger;

        public PlivoWhatsAppInterviewController(
            IConfiguration           configuration,
            IPlivoWhatsAppService    whatsAppService,
            IPlivoWhatsAppRepository repository,
            IApplicationErrorLogger  logger)
        {
            _configuration   = configuration;
            _whatsAppService = whatsAppService;
            _repository      = repository;
            _logger          = logger;
        }

        // ─── Dashboard ───────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index(
            int    page        = 1,
            int    pageSize    = 50,
            string status      = null,
            string purpose     = null,
            string meeting     = null,
            string disposition = null)
        {
            try
            {
                if (page     < 1)   page     = 1;
                if (pageSize < 10)  pageSize = 10;
                if (pageSize > 200) pageSize = 200;

                var logs  = await _repository.GetLogDetailsAsync(page, pageSize, status, purpose, meeting, disposition);
                var first = logs.FirstOrDefault();
                int total = first?.TotalCount ?? 0;
                int pages = total > 0 ? (int)Math.Ceiling((double)total / pageSize) : 1;

                ViewBag.Page        = page;
                ViewBag.PageSize    = pageSize;
                ViewBag.TotalCount  = total;
                ViewBag.TotalPages  = pages;
                ViewBag.Status      = status;
                ViewBag.Purpose     = purpose;
                ViewBag.Meeting     = meeting;
                ViewBag.Disposition = disposition;

                return View(logs);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                ViewBag.Page = 1; ViewBag.PageSize = pageSize; ViewBag.TotalCount = 0; ViewBag.TotalPages = 1;
                ViewBag.Status = status; ViewBag.Purpose = purpose; ViewBag.Meeting = meeting; ViewBag.Disposition = disposition;
                return View(new System.Collections.Generic.List<Risebird.Entities.ViewModel.PlivoWhatsAppLogDetailVM>());
            }
        }

        // ─── Manual message triggers ─────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiateScheduleMessage(
            string recipientName,
            string recipientPhone,
            string recipientEmail,
            int?   irfId,
            int?   candidateId,
            string jobTitle   = null,
            string clientName = null)
        {
            try
            {
                var userEmail = User.Identity?.Name ?? "Unknown";

                var log = new PlivoWhatsAppLog
                {
                    RecipientName   = recipientName,
                    RecipientPhone  = recipientPhone,
                    RecipientEmail  = recipientEmail,
                    IRFId           = irfId,
                    CandidateId     = candidateId,
                    JobTitle        = jobTitle,
                    ClientName      = clientName,
                    MessagePurpose  = "schedule",
                    MessageStatus   = "initiated",
                    Status          = "initiated",
                    TriggeredBy     = userEmail
                };
                int logId = await _repository.SaveLogAsync(log);
                log.Id = logId;

                var context = BuildTriggerContext(log, logId, null);
                var response = await _whatsAppService.SendScheduleMessageAsync(context);

                // Checkpoint — log raw trigger request + response
                try
                {
                    await _repository.SaveDebugLogAsync(new PlivoWhatsAppDebugLog
                    {
                        LogType          = "trigger-request",
                        HttpStatusCode   = response?.HttpStatusCode,
                        PlivoCXId        = response?.ApiId,
                        LogId            = logId,
                        RawPayload       = JsonConvert.SerializeObject(context),
                        ProcessingStatus = string.IsNullOrEmpty(response?.Error) ? "success" : "failed",
                        ErrorMessage     = response?.Error
                    });
                    await _repository.SaveDebugLogAsync(new PlivoWhatsAppDebugLog
                    {
                        LogType          = "trigger-response",
                        HttpStatusCode   = response?.HttpStatusCode,
                        PlivoCXId        = response?.ApiId,
                        LogId            = logId,
                        RawPayload       = response?.RawResponseBody,
                        ProcessingStatus = string.IsNullOrEmpty(response?.Error) ? "success" : "failed",
                        ErrorMessage     = response?.Error
                    });
                }
                catch { }

                if (!string.IsNullOrEmpty(response?.ConversationId))
                {
                    log.PlivoCXConversationId = response.ConversationId;
                    log.PlivoCXRequestId      = response.ApiId;
                    log.MessageStatus         = "triggered";
                    log.Status                = "triggered";
                    log.UpdatedAt             = DateTime.UtcNow;
                    await _repository.UpdateLogAsync(log);
                    TempData["Success"] = $"WhatsApp conversation initiated for {recipientName}. Conversation ID: {response.ConversationId}";
                }
                else if (!string.IsNullOrEmpty(response?.Error))
                {
                    log.MessageStatus = "trigger-failed";
                    log.Status        = "trigger-failed";
                    log.BookingApiResponse = response.Error;
                    log.UpdatedAt     = DateTime.UtcNow;
                    await _repository.UpdateLogAsync(log);
                    TempData["Error"] = $"Trigger failed: {response.Error}";
                }
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["Error"] = $"Error: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiateRescheduleMessage(
            string recipientName,
            string recipientPhone,
            string recipientEmail,
            int?   irfId,
            int?   candidateId,
            string jobTitle              = null,
            string clientName            = null,
            string originalInterviewDate = null)
        {
            try
            {
                var userEmail = User.Identity?.Name ?? "Unknown";

                var log = new PlivoWhatsAppLog
                {
                    RecipientName         = recipientName,
                    RecipientPhone        = recipientPhone,
                    RecipientEmail        = recipientEmail,
                    IRFId                 = irfId,
                    CandidateId           = candidateId,
                    JobTitle              = jobTitle,
                    ClientName            = clientName,
                    MessagePurpose        = "reschedule",
                    OriginalInterviewDate = originalInterviewDate,
                    MessageStatus         = "initiated",
                    Status                = "initiated",
                    TriggeredBy           = userEmail
                };
                int logId = await _repository.SaveLogAsync(log);
                log.Id = logId;

                var context  = BuildTriggerContext(log, logId, originalInterviewDate);
                var response = await _whatsAppService.SendRescheduleMessageAsync(context);

                // Checkpoint — log raw reschedule trigger request + response
                try
                {
                    await _repository.SaveDebugLogAsync(new PlivoWhatsAppDebugLog
                    {
                        LogType          = "reschedule-trigger-request",
                        HttpStatusCode   = response?.HttpStatusCode,
                        PlivoCXId        = response?.ApiId,
                        LogId            = logId,
                        RawPayload       = JsonConvert.SerializeObject(context),
                        ProcessingStatus = string.IsNullOrEmpty(response?.Error) ? "success" : "failed",
                        ErrorMessage     = response?.Error
                    });
                    await _repository.SaveDebugLogAsync(new PlivoWhatsAppDebugLog
                    {
                        LogType          = "reschedule-trigger-response",
                        HttpStatusCode   = response?.HttpStatusCode,
                        PlivoCXId        = response?.ApiId,
                        LogId            = logId,
                        RawPayload       = response?.RawResponseBody,
                        ProcessingStatus = string.IsNullOrEmpty(response?.Error) ? "success" : "failed",
                        ErrorMessage     = response?.Error
                    });
                }
                catch { }

                if (!string.IsNullOrEmpty(response?.ConversationId))
                {
                    log.PlivoCXConversationId = response.ConversationId;
                    log.PlivoCXRequestId      = response.ApiId;
                    log.MessageStatus         = "triggered";
                    log.Status                = "triggered";
                    log.UpdatedAt             = DateTime.UtcNow;
                    await _repository.UpdateLogAsync(log);
                    TempData["Success"] = $"WhatsApp reschedule conversation initiated for {recipientName}.";
                }
                else if (!string.IsNullOrEmpty(response?.Error))
                {
                    log.MessageStatus      = "trigger-failed";
                    log.Status             = "trigger-failed";
                    log.BookingApiResponse = response.Error;
                    log.UpdatedAt          = DateTime.UtcNow;
                    await _repository.UpdateLogAsync(log);
                    TempData["Error"] = $"Reschedule trigger failed: {response.Error}";
                }
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["Error"] = $"Error: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        // ─── Plivo CX webhook ─────────────────────────────────────────────────────

        /// <summary>
        /// Plivo CX posts here when the WhatsApp AI conversation ends.
        /// The payload is identical in shape to the voice HangupCallback envelope —
        /// same nested JSON with event_data containing AI-extracted variables.
        ///
        /// Configure in cx.plivo.com → Agents → Your WhatsApp Flow → Agent Config → Events → Hangup URL:
        ///   https://your-domain.com/PlivoWhatsAppInterview/ConversationEndCallback
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ConversationEndCallback()
        {
            // ── Step 1: Record that the webhook arrived ──────────────────────────
            await SaveCpAsync(WaCp.CallbackReceived, "ConversationEndCallback HTTP POST received");

            // ── Step 2: Read raw body ────────────────────────────────────────────
            string rawBody = string.Empty;
            try
            {
                Request.EnableBuffering();
                using var reader = new System.IO.StreamReader(
                    Request.Body, System.Text.Encoding.UTF8, leaveOpen: true);
                rawBody = await reader.ReadToEndAsync();
                Request.Body.Position = 0;
                await SaveCpAsync(WaCp.CallbackBodyRead,
                    $"Body read — {rawBody?.Length ?? 0} chars",
                    extra: rawBody?.Substring(0, Math.Min(500, rawBody?.Length ?? 0)));
            }
            catch (Exception ex)
            {
                await SaveCpAsync(WaCp.CallbackParseFailed, $"Body read FAILED: {ex.Message}");
            }

            // ── Step 3: Always persist the raw callback payload ──────────────────
            try
            {
                await _repository.SaveDebugLogAsync(new PlivoWhatsAppDebugLog
                {
                    LogType          = "wa-callback",
                    HttpStatusCode   = 0,
                    RawPayload       = rawBody,
                    ProcessingStatus = "received"
                });
            }
            catch { }

            // ── Step 4: Parse the nested Plivo CX envelope ───────────────────────
            // { "data": { "object": { "conversation_id": "...", "event_data": { "NodeName.variable": "value" } } } }
            PlivoCXWhatsAppPayload payload = null;

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(rawBody);
                var obj       = doc.RootElement
                                   .GetProperty("data")
                                   .GetProperty("object");

                var ed = new System.Collections.Generic.Dictionary<string, string>();
                if (obj.TryGetProperty("event_data", out var eventData))
                    foreach (var kv in eventData.EnumerateObject())
                        ed[kv.Name] = kv.Value.GetString();

                payload = new PlivoCXWhatsAppPayload
                {
                    ConversationId       = obj.TryGetProperty("conversation_id", out var cid) ? cid.GetString() : null,
                    CallStatus           = GetEdValue(ed, "call_status"),
                    Transcript           = GetEdValue(ed, "transcript"),
                    Summary              = GetEdValue(ed, "summary"),
                    CandidateName        = GetEdValue(ed, "candidate_name"),
                    CandidateEmail       = GetEdValue(ed, "candidate_email"),
                    IrfId                = GetEdValue(ed, "irf_id"),
                    LogId                = GetEdValue(ed, "log_id"),
                    InterviewDate        = GetEdValue(ed, "interview_date"),
                    InterviewTime        = GetEdValue(ed, "interview_time"),
                    CandidateConfirmed   = GetEdValue(ed, "candidate_confirmed"),
                    CandidateDisposition = GetEdValue(ed, "candidate_disposition")
                };
                await SaveCpAsync(WaCp.CallbackJsonParsed,
                    $"Parsed as JSON — conv_id: {payload.ConversationId}, log_id: {payload.LogId}",
                    convId: payload.ConversationId,
                    extra: $"confirmed: {payload.CandidateConfirmed}, date: {payload.InterviewDate}, time: {payload.InterviewTime}");
            }
            catch (Exception ex)
            {
                await SaveCpAsync(WaCp.CallbackParseFailed, $"JSON parse FAILED: {ex.Message}");
            }

            // ── Step 5: Fallback to form-encoded ─────────────────────────────────
            if (payload == null || (string.IsNullOrEmpty(payload.ConversationId) && string.IsNullOrEmpty(payload.LogId)))
            {
                try
                {
                    var form = await Request.ReadFormAsync();
                    payload = new PlivoCXWhatsAppPayload
                    {
                        ConversationId       = form["conversation_id"],
                        CallStatus           = form["call_status"],
                        Transcript           = form["transcript"],
                        Summary              = form["summary"],
                        CandidateName        = form["candidate_name"],
                        CandidateEmail       = form["candidate_email"],
                        IrfId                = form["irf_id"],
                        LogId                = form["log_id"],
                        InterviewDate        = form["interview_date"],
                        InterviewTime        = form["interview_time"],
                        CandidateConfirmed   = form["candidate_confirmed"],
                        CandidateDisposition = form["candidate_disposition"]
                    };
                    await SaveCpAsync(WaCp.CallbackFormParsed,
                        $"Parsed as form-encoded — conv_id: {payload.ConversationId}, log_id: {payload.LogId}",
                        convId: payload.ConversationId,
                        extra: $"keys: {string.Join(", ", form.Keys)}");
                }
                catch (Exception ex)
                {
                    await SaveCpAsync(WaCp.CallbackParseFailed, $"Form parse FAILED: {ex.Message}");
                }
            }

            // ── Step 6: Validate we have usable identifiers ───────────────────────
            if (payload == null ||
                (string.IsNullOrEmpty(payload.ConversationId) && string.IsNullOrEmpty(payload.LogId)))
            {
                await SaveCpAsync(WaCp.CallbackParseFailed,
                    "No conv_id or log_id after all parse attempts — dropping webhook",
                    extra: $"raw (first 300): {rawBody?.Substring(0, Math.Min(300, rawBody?.Length ?? 0))}");
                return Ok();
            }

            try
            {
                // ── Step 7: Find matching log ────────────────────────────────────
                PlivoWhatsAppLog log  = null;
                string           how  = null;

                if (!string.IsNullOrEmpty(payload.LogId) && int.TryParse(payload.LogId, out int parsedLogId))
                {
                    log = await _repository.GetLogByIdAsync(parsedLogId);
                    how = $"log_id={parsedLogId}";
                }
                if (log == null && !string.IsNullOrEmpty(payload.ConversationId))
                {
                    log = await _repository.GetLogByConversationIdAsync(payload.ConversationId);
                    how = $"conversation_id={payload.ConversationId}";
                }

                if (log == null)
                {
                    await SaveCpAsync(WaCp.LogNotFound,
                        $"No WhatsApp log matched — tried: {how}",
                        convId: payload.ConversationId);
                    return Ok();
                }

                await SaveCpAsync(WaCp.LogFound,
                    $"WhatsApp log #{log.Id} found via {how}",
                    convId: payload.ConversationId,
                    logId: log.Id,
                    extra: $"phone: {log.RecipientPhone}, status_was: {log.Status}");

                // ── Step 8: Apply conversation outcome ───────────────────────────
                log.PlivoCXConversationId   = payload.ConversationId ?? log.PlivoCXConversationId;
                log.MessageStatus           = "completed";
                log.Status                  = "completed";
                log.ConversationTranscript  = payload.Transcript;
                log.ConversationSummary     = payload.Summary;
                log.AIReportedInterviewDate = ResolveRelativeDate(payload.InterviewDate);
                log.AIReportedInterviewTime = payload.InterviewTime;
                log.CandidateDisposition    = NormaliseDisposition(payload.CandidateDisposition);
                log.ConversationEndedAt     = DateTime.UtcNow;
                log.UpdatedAt               = DateTime.UtcNow;

                await SaveCpAsync(WaCp.DispositionExtracted,
                    $"Disposition: {log.CandidateDisposition ?? "(none)"} — raw: {payload.CandidateDisposition}",
                    convId: payload.ConversationId, logId: log.Id);

                bool candidateConfirmed = string.Equals(
                    payload.CandidateConfirmed, "true", StringComparison.OrdinalIgnoreCase);
                log.AIReportedBooked = candidateConfirmed;

                if (candidateConfirmed &&
                    !string.IsNullOrWhiteSpace(payload.InterviewDate) &&
                    !string.IsNullOrWhiteSpace(payload.InterviewTime))
                {
                    await ProcessBookingAsync(log, payload);
                }
                else
                {
                    await SaveCpAsync(WaCp.BookingSkipped,
                        $"Booking skipped — confirmed: {candidateConfirmed}, " +
                        $"date: '{payload.InterviewDate}', time: '{payload.InterviewTime}'",
                        convId: payload.ConversationId, logId: log.Id);
                }

                await _repository.UpdateLogAsync(log);

                if (log.CandidateId.HasValue && !string.IsNullOrEmpty(log.CandidateDisposition))
                    await _repository.UpdateCandidateDispositionAsync(log.CandidateId.Value, log.CandidateDisposition);

                await SaveCpAsync(WaCp.CallbackComplete,
                    $"WhatsApp log #{log.Id} updated — booked: {log.InterviewBooked}, " +
                    $"booking_success: {log.BookingApiSuccess}",
                    convId: payload.ConversationId, logId: log.Id);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                await SaveCpAsync(WaCp.CallbackParseFailed,
                    $"Unhandled exception in ConversationEndCallback: {ex.Message}",
                    convId: payload?.ConversationId,
                    extra: ex.ToString());
            }

            return Ok();
        }

        // ─── Booking logic (identical to voice controller, with checkpoints) ─────

        private async Task ProcessBookingAsync(PlivoWhatsAppLog log, PlivoCXWhatsAppPayload payload)
        {
            var convId = payload.ConversationId;
            try
            {
                var irfIdStr       = payload.IrfId ?? log.IRFId?.ToString();
                var candidateEmail = payload.CandidateEmail ?? log.RecipientEmail;

                if (string.IsNullOrEmpty(candidateEmail) && log.CandidateId.HasValue)
                    candidateEmail = await _repository.GetCandidateEmailByIdAsync(log.CandidateId.Value);

                if (string.IsNullOrEmpty(candidateEmail) ||
                    string.IsNullOrEmpty(irfIdStr) ||
                    !int.TryParse(irfIdStr, out int irfId))
                {
                    log.BookingApiResponse = "Missing candidateEmail or irfId — booking skipped.";
                    await SaveCpAsync(WaCp.BookingSkipped,
                        $"Missing fields — email: '{candidateEmail}', irfId: '{irfIdStr}'",
                        convId: convId, logId: log.Id);
                    return;
                }

                // Parse date+time (same multi-format logic as voice)
                var resolvedDate   = ResolveRelativeDate(payload.InterviewDate);
                var dateTimeString = $"{resolvedDate} {payload.InterviewTime}";

                string[] formats =
                {
                    "yyyy-MM-dd hh:mm tt",  "yyyy-MM-dd h:mm tt",
                    "yyyy-MM-dd HH:mm",     "yyyy-MM-dd H:mm",
                    "dddd, dd MMMM yyyy hh:mm tt", "dddd, dd MMMM yyyy h:mm tt",
                    "dddd, dd MMMM yyyy HH:mm",    "dddd, dd MMMM yyyy H:mm",
                    "dd MMMM yyyy hh:mm tt", "dd MMMM yyyy HH:mm",
                    "dd/MM/yyyy hh:mm tt",   "dd/MM/yyyy HH:mm"
                };

                DateTime? parsedDateTime = null;
                foreach (var fmt in formats)
                    if (DateTime.TryParseExact(dateTimeString, fmt,
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out var dt))
                    { parsedDateTime = dt; break; }
                if (!parsedDateTime.HasValue && DateTime.TryParse(dateTimeString, out var dtFallback))
                    parsedDateTime = dtFallback;

                if (!parsedDateTime.HasValue)
                {
                    log.BookingApiResponse = $"Could not parse datetime: '{dateTimeString}'";
                    await SaveCpAsync(WaCp.BookingSkipped,
                        $"DateTime parse failed: '{dateTimeString}'",
                        convId: convId, logId: log.Id);
                    return;
                }

                log.BookedInterviewDate     = parsedDateTime.Value.ToString("yyyy-MM-dd");
                log.BookedInterviewDateTime = parsedDateTime.Value;
                log.InterviewBooked         = true;

                var hasConfirmed = await _repository.HasConfirmedInterviewAsync(candidateEmail, irfId);
                if (hasConfirmed)
                {
                    log.BookingApiResponse = "Already confirmed — duplicate skipped.";
                    await SaveCpAsync(WaCp.BookingSkipped,
                        "Duplicate booking skipped — interview already confirmed",
                        convId: convId, logId: log.Id);
                    return;
                }

                await Task.Delay(2000);

                var candidateDetails = await _repository.GetCandidateDetailsForScheduleAsync(irfId, candidateEmail);
                if (candidateDetails == null)
                {
                    log.BookingApiResponse = "Candidate details not found in DB.";
                    await SaveCpAsync(WaCp.BookingSkipped,
                        $"Candidate details not found — irfId: {irfId}, email: {candidateEmail}",
                        convId: convId, logId: log.Id);
                    return;
                }

                var roundedDate = parsedDateTime.Value.RoundToNearSlot(candidateDetails.duration);
                log.BookedInterviewDate     = roundedDate.ToString("yyyy-MM-dd");
                log.BookedInterviewTime     = roundedDate.ToString("HH:mm");
                log.BookedInterviewDateTime = roundedDate;

                var dto = new ScheduleCandidatesRequest
                {
                    skillChannelId = candidateDetails.IRFId,
                    requestedBy    = candidateDetails.ClientHRAttemptContact,
                    requestType    = log.MessagePurpose == "reschedule"
                                        ? "reschedule_voiceAgent"
                                        : "schedule_voiceAgent"
                };
                dto.CandidateList.Add(new ScheduleCandidateList
                {
                    EmailId       = candidateEmail,
                    Interviewdate = roundedDate,
                    ResumeLink    = candidateDetails.ResumeLink,
                    Phone         = candidateDetails.Phone,
                    Name          = candidateDetails.CandidateName
                });

                await SaveCpAsync(WaCp.BookingStarted,
                    $"Calling Schedule API — irfId: {irfId}, email: {candidateEmail}, " +
                    $"slot: {roundedDate:yyyy-MM-dd HH:mm}",
                    convId: convId, logId: log.Id);

                const string apiUrl = "https://api.portal.risebird.io/api/Candidates/Schedule";
                var restClient  = new RestClient(apiUrl);
                var restRequest = new RestRequest(apiUrl, Method.Post);
                restRequest.AddHeader("content-type", "application/json");
                restRequest.AddHeader("x-functions-key", "SJJvpXEzy-1fOQN2mygBaTT6gXctL406VhpMwn0BqhKUAzFuBDDP5Q==");
                restRequest.AddHeader("tenant-id", candidateDetails.tenantId);
                restRequest.AddParameter("application/json", JsonConvert.SerializeObject(dto), ParameterType.RequestBody);

                var apiResponse = await restClient.ExecuteAsync(restRequest);
                log.BookingApiResponse = apiResponse.Content;
                log.BookingApiSuccess  = apiResponse.IsSuccessful;

                await SaveCpAsync(WaCp.BookingDone,
                    $"Schedule API — status: {apiResponse.StatusCode}, success: {apiResponse.IsSuccessful}",
                    convId: convId, logId: log.Id,
                    extra: $"response: {apiResponse.Content?.Substring(0, Math.Min(500, apiResponse.Content?.Length ?? 0))}");
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                log.BookingApiResponse = $"Exception: {ex.Message}";
                await SaveCpAsync(WaCp.BookingException,
                    $"Exception: {ex.Message}", convId: convId, logId: log.Id,
                    extra: ex.ToString());
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private static string GetEdValue(
            System.Collections.Generic.Dictionary<string, string> ed, string key)
        {
            if (ed == null || string.IsNullOrEmpty(key)) return null;
            if (ed.TryGetValue(key, out var exact)) return exact;
            var suffix = "." + key;
            foreach (var kv in ed)
                if (kv.Key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return kv.Value;
            return null;
        }

        private static string ResolveRelativeDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            return raw.ToLowerInvariant().Trim() switch
            {
                "tomorrow"                              => DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
                "day after tomorrow" or "dayaftertomorrow" => DateTime.Now.AddDays(2).ToString("yyyy-MM-dd"),
                _                                       => raw
            };
        }

        private static string NormaliseDisposition(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            return raw.ToLowerInvariant().Trim() switch
            {
                "interested"                                                     => "interested",
                "uninterested" or "not interested" or "disinterested"           => "uninterested",
                "cant_say" or "cant say" or "can't say" or "unsure" or "unclear" => "cant_say",
                _                                                                => "cant_say"
            };
        }

        private static PlivoWhatsAppTriggerContext BuildTriggerContext(
            PlivoWhatsAppLog log, int logId, string originalInterviewDate)
        {
            return new PlivoWhatsAppTriggerContext
            {
                CandidateName         = log.RecipientName,
                CandidatePhone        = log.RecipientPhone,
                CandidateEmail        = log.RecipientEmail,
                IrfId                 = log.IRFId ?? 0,
                CandidateId           = log.CandidateId,
                JobTitle              = log.JobTitle,
                ClientName            = log.ClientName,
                MessagePurpose        = log.MessagePurpose,
                OriginalInterviewDate = originalInterviewDate,
                TriggeredBy           = log.TriggeredBy,
                LogId                 = logId.ToString(),
                TodayDate             = DateTime.Now.ToString("dddd, dd MMMM yyyy"),
                TomorrowDateLabel     = DateTime.Now.AddDays(1).ToString("dddd, dd MMMM yyyy"),
                DayAfterDateLabel     = DateTime.Now.AddDays(2).ToString("dddd, dd MMMM yyyy")
            };
        }

        // ─── Shared checkpoint helper ─────────────────────────────────────────────

        private async Task SaveCpAsync(
            string name, string msg,
            string convId = null, int? logId = null, string extra = null)
        {
            try
            {
                await _repository.SaveCheckpointAsync(new PlivoWhatsAppCheckpoint
                {
                    CheckpointName = name,
                    Message        = msg,
                    ConversationId = convId,
                    LogId          = logId,
                    AdditionalData = extra
                });
            }
            catch { }
        }

        // ─── Management endpoints ─────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetLogs(int count = 50)
            => Json(await _repository.GetRecentLogsAsync(count));

        [HttpGet]
        public async Task<IActionResult> GetLog(int id)
        {
            var log = await _repository.GetLogByIdAsync(id);
            return log != null ? Json(log) : NotFound();
        }

        /// <summary>
        /// View recent raw trigger request/response + callback payloads.
        /// Access: /PlivoWhatsAppInterview/DebugLogs?count=50
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DebugLogs(int count = 50)
        {
            try
            {
                var logs = await _repository.GetRecentDebugLogsAsync(count);
                return Json(logs);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        /// <summary>
        /// View recent granular processing checkpoints for ConversationEndCallback.
        /// Access: /PlivoWhatsAppInterview/Checkpoints?count=100
        /// Each row is a named step — use to trace exactly where processing stopped.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Checkpoints(int count = 100)
        {
            try
            {
                var checkpoints = await _repository.GetRecentCheckpointsAsync(count);
                return Json(checkpoints);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult WebhookInfo()
        {
            return Json(new
            {
                status              = "ok",
                conversationEndUrl  = $"{Request.Scheme}://{Request.Host}/PlivoWhatsAppInterview/ConversationEndCallback",
                instructions = new[]
                {
                    "1. Go to cx.plivo.com → AI Studio → your WhatsApp flow",
                    "2. Click Agent Config → Events → set Hangup URL to the conversationEndUrl above",
                    "3. Copy the WhatsApp flow's API Trigger URL → set as PlivoWhatsApp:ScheduleTriggerEndpointUrl in appsettings.json",
                    "4. Create a separate reschedule flow → copy its trigger URL → set as PlivoWhatsApp:RescheduleTriggerEndpointUrl",
                    "5. Ensure the AI Conversation node extracts: interview_date, interview_time, candidate_confirmed, candidate_disposition"
                }
            });
        }
    }

    // ── Internal payload model for ConversationEndCallback ────────────────────────
    public class PlivoCXWhatsAppPayload
    {
        public string ConversationId       { get; set; }
        public string CallStatus           { get; set; }
        public string Transcript           { get; set; }
        public string Summary              { get; set; }
        public string CandidateName        { get; set; }
        public string CandidateEmail       { get; set; }
        public string IrfId                { get; set; }
        public string LogId                { get; set; }
        public string InterviewDate        { get; set; }
        public string InterviewTime        { get; set; }
        public string CandidateConfirmed   { get; set; }
        public string CandidateDisposition { get; set; }
    }
}
