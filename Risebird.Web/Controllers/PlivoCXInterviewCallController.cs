using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using Risebird.Web.Extensions;
using System;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// Controller for Plivo CX AI Agent interview scheduling.
    ///
    /// Architecture (much simpler than the Programmable Voice approach):
    ///
    ///   1. InitiateInterviewCall
    ///        → saves PlivoCXCallLog row → gets logId
    ///        → POSTs to Plivo CX API trigger (via IPlivoCXService) with candidate variables + logId
    ///
    ///   2. Plivo CX handles the entire call internally:
    ///        • Initiates outbound call from the Indian number
    ///        • AI Conversation node drives the interview scheduling dialogue
    ///        • Extracts variables: interview_date, interview_time, candidate_confirmed
    ///
    ///   3. HangupCallback  (Plivo CX → our server, configured as "Hangup URL" in Events tab)
    ///        → receives all extracted variables + transcript
    ///        → if candidate_confirmed=true → calls external Schedule API
    ///        → updates PlivoCXCallLog
    ///
    /// No WebSocket bridge, no audio conversion, no OpenAI key needed here.
    /// The AI instructions live in the AI Conversation node inside your Plivo CX flow.
    /// </summary>
    [Authorize]
    public class PlivoCXInterviewCallController : Controller
    {
        private readonly IConfiguration        _configuration;
        private readonly IPlivoCXService       _plivoCXService;
        private readonly IPlivoCXCallRepository _repository;
        private readonly IApplicationErrorLogger _logger;

        public PlivoCXInterviewCallController(
            IConfiguration           configuration,
            IPlivoCXService          plivoCXService,
            IPlivoCXCallRepository   repository,
            IApplicationErrorLogger  applicationErrorLogger)
        {
            _configuration  = configuration;
            _plivoCXService = plivoCXService;
            _repository     = repository;
            _logger         = applicationErrorLogger;
        }

        // ─── Dashboard ───────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var calls = await _repository.GetRecentCallLogsAsync(100);
                return View(calls);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return View(new System.Collections.Generic.List<PlivoCXCallLog>());
            }
        }

        // ─── Manual call trigger ─────────────────────────────────────────────────

        /// <summary>
        /// Manually initiate a single outbound interview call via Plivo CX.
        /// Called from the Index view form.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiateInterviewCall(
            string recipientName,
            string recipientPhone,
            string recipientEmail,
            int?   irfId,
            int?   candidateId,
            string jobTitle  = null,
            string clientName = null)
        {
            try
            {
                var userEmail  = User.Identity?.Name ?? "Unknown";

                // 1. Try to resolve richer context from DB
                if (irfId.HasValue && !string.IsNullOrEmpty(recipientEmail))
                {
                    try
                    {
                        var details = await _repository.GetCandidateDetailsForScheduleAsync(irfId.Value, recipientEmail);
                        if (details != null)
                        {
                            jobTitle   ??= string.Empty;
                            clientName ??= details.ClientHRAttemptContact;
                        }
                    }
                    catch { }
                }

                // 2. Save initial call log to get an Id we can pass to Plivo CX as a variable
                var callLog = new PlivoCXCallLog
                {
                    RecipientName  = recipientName,
                    RecipientPhone = recipientPhone,
                    RecipientEmail = recipientEmail,
                    IRFId          = irfId,
                    CandidateId    = candidateId,
                    JobTitle       = jobTitle,
                    ClientName     = clientName,
                    CallStatus     = "initiated",
                    InterviewBooked = false,
                    TriggeredBy    = userEmail
                };
                int logId = await _repository.SaveCallLogAsync(callLog);
                callLog.Id = logId;

                // 3. POST to Plivo CX trigger
                var triggerRequest = new PlivoCXTriggerRequest
                {
                    PhoneNumber    = recipientPhone,
                    CandidateName  = recipientName ?? "Candidate",
                    CandidateEmail = recipientEmail,
                    IrfId          = irfId?.ToString(),
                    JobTitle       = jobTitle,
                    ClientName     = clientName,
                    LogId          = logId.ToString()
                };

                var response = await _plivoCXService.TriggerOutboundCallAsync(triggerRequest);

                // 3b. Checkpoint — log the raw trigger request + response
                await _repository.SaveWebhookDebugLogAsync(new PlivoCXWebhookDebugLog
                {
                    LogType          = "trigger-request",
                    HttpStatusCode   = response?.HttpStatusCode,
                    PlivoCXId        = response?.ApiId,
                    CallLogId        = logId,
                    RawPayload       = JsonConvert.SerializeObject(triggerRequest),
                    ProcessingStatus = string.IsNullOrEmpty(response?.Error) ? "success" : "failed",
                    ErrorMessage     = response?.Error
                });
                await _repository.SaveWebhookDebugLogAsync(new PlivoCXWebhookDebugLog
                {
                    LogType          = "trigger-response",
                    HttpStatusCode   = response?.HttpStatusCode,
                    PlivoCXId        = response?.ApiId,
                    CallLogId        = logId,
                    RawPayload       = response?.RawResponseBody,
                    ProcessingStatus = string.IsNullOrEmpty(response?.Error) ? "success" : "failed",
                    ErrorMessage     = response?.Error
                });

                // 4. Update log with conversation id returned by Plivo CX
                if (!string.IsNullOrEmpty(response?.ConversationId))
                {
                    callLog.PlivoCXConversationId = response.ConversationId;
                    callLog.PlivoCXRequestId      = response.ApiId;
                    callLog.CallStatus            = "triggered";
                    callLog.UpdatedAt             = DateTime.UtcNow;
                    await _repository.UpdateCallLogAsync(callLog);
                }
                else if (!string.IsNullOrEmpty(response?.Error))
                {
                    callLog.CallStatus  = "trigger-failed";
                    callLog.HangupCause = response.Error;
                    callLog.UpdatedAt   = DateTime.UtcNow;
                    await _repository.UpdateCallLogAsync(callLog);

                    TempData["Error"] = $"Plivo CX trigger failed: {response.Error}";
                    return RedirectToAction("Index");
                }

                TempData["Success"] = $"Call initiated for {recipientName} ({recipientPhone}). Conversation ID: {response?.ConversationId}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // ─── Plivo CX webhooks ────────────────────────────────────────────────────

        /// <summary>
        /// Plivo CX posts here when a call ends (configured as "Hangup URL" in Events tab
        /// of your agent configuration in cx.plivo.com).
        ///
        /// The payload contains:
        ///   • System variables: conversation_id, call_status, duration, hangup_cause, transcript
        ///   • Variables we sent to the trigger: candidate_name, candidate_email, irf_id, log_id
        ///   • Variables extracted by AI Conversation node: interview_date, interview_time, candidate_confirmed
        ///
        /// This is where we call the external Schedule API to actually book the interview.
        ///
        /// Configure this URL in Plivo CX:
        ///   cx.plivo.com → AI Studio → Your Flow → Agent Config → Events → Hangup URL
        ///   Value: https://your-domain.com/PlivoCXInterviewCall/HangupCallback
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> HangupCallback()
        {
            // Helper to fire-and-forget a checkpoint without ever throwing
            async Task Cp(string name, string msg, string convId = null, int? callLogId = null, string extra = null)
            {
                try
                {
                    await _repository.SaveCheckpointAsync(new PlivoCXWebhookCheckpoint
                    {
                        CheckpointName = name,
                        Message        = msg,
                        ConversationId = convId,
                        CallLogId      = callLogId,
                        AdditionalData = extra
                    });
                }
                catch { }
            }

            await Cp(CheckpointNames.HangupReceived, $"HangupCallback hit — Content-Type: {Request.ContentType}");

            // ── Step 1: Read raw body ────────────────────────────────────────────────
            string rawBody = string.Empty;
            try
            {
                Request.EnableBuffering();
                using var reader = new System.IO.StreamReader(
                    Request.Body, System.Text.Encoding.UTF8, leaveOpen: true);
                rawBody = await reader.ReadToEndAsync();
                Request.Body.Position = 0;
                await Cp(CheckpointNames.HangupBodyRead,
                    $"Raw body read — length: {rawBody.Length}",
                    extra: rawBody.Length > 500 ? rawBody.Substring(0, 500) + "…" : rawBody);
            }
            catch (Exception ex)
            {
                await Cp(CheckpointNames.HangupBodyRead, $"Body read FAILED: {ex.Message}");
            }

            // ── Step 2: Parse — try JSON first, then form-encoded ────────────────────
            PlivoCXHangupPayload payload = null;
            bool parsedAsJson = false;

            try
            {
                payload = JsonConvert.DeserializeObject<PlivoCXHangupPayload>(rawBody);
                if (payload != null &&
                    (!string.IsNullOrEmpty(payload.ConversationId) || !string.IsNullOrEmpty(payload.LogId)))
                {
                    parsedAsJson = true;
                    await Cp(CheckpointNames.HangupJsonParsed,
                        $"Parsed as JSON — conv_id: {payload.ConversationId}, log_id: {payload.LogId}",
                        convId: payload.ConversationId);
                }
            }
            catch { }

            if (!parsedAsJson)
            {
                // Fallback: form-urlencoded
                try
                {
                    var form = await Request.ReadFormAsync();
                    payload = new PlivoCXHangupPayload
                    {
                        ConversationId         = form["conversation_id"],
                        CallUUID               = form["call_uuid"],
                        CallStatus             = form["call_status"],
                        Duration               = form["duration"],
                        HangupCause            = form["hangup_cause"],
                        Transcript             = form["transcript"],
                        Summary                = form["summary"],
                        PhoneNumber            = form["phone_number"],
                        CandidateName          = form["candidate_name"],
                        CandidateEmail         = form["candidate_email"],
                        IrfId                  = form["irf_id"],
                        JobTitle               = form["job_title"],
                        ClientName             = form["client_name"],
                        LogId                  = form["log_id"],
                        InterviewDate          = form["interview_date"],
                        InterviewTime          = form["interview_time"],
                        CandidateConfirmed     = form["candidate_confirmed"],
                        ConfirmedCandidateName = form["confirmed_candidate_name"]
                    };
                    await Cp(CheckpointNames.HangupFormParsed,
                        $"Parsed as form-encoded — conv_id: {payload.ConversationId}, log_id: {payload.LogId}",
                        convId: payload.ConversationId,
                        extra: $"keys: {string.Join(", ", form.Keys)}");
                }
                catch (Exception ex)
                {
                    await Cp(CheckpointNames.HangupParseFailed, $"Form parse FAILED: {ex.Message}");
                }
            }

            // ── Step 3: Always write the raw debug log ───────────────────────────────
            try
            {
                int? logIdHint = null;
                if (!string.IsNullOrEmpty(payload?.LogId) && int.TryParse(payload.LogId, out int parsedId))
                    logIdHint = parsedId;

                await _repository.SaveWebhookDebugLogAsync(new PlivoCXWebhookDebugLog
                {
                    LogType          = "hangup-callback",
                    HttpStatusCode   = 0,
                    PlivoCXId        = payload?.ConversationId ?? payload?.LogId,
                    CallLogId        = logIdHint,
                    RawPayload       = rawBody,
                    ProcessingStatus = "received"
                });
            }
            catch { }

            // ── Step 4: Validate we got usable identifiers ────────────────────────────
            if (payload == null ||
                (string.IsNullOrEmpty(payload.ConversationId) && string.IsNullOrEmpty(payload.LogId)))
            {
                await Cp(CheckpointNames.HangupParseFailed,
                    "No conv_id or log_id — dropping webhook",
                    extra: $"raw (first 200): {rawBody?.Substring(0, Math.Min(200, rawBody?.Length ?? 0))}");
                return Ok();
            }

            try
            {
                // ── Step 5: Find matching call log ───────────────────────────────────
                // Priority: log_id (our DB PK) → conversation_id → fallback to request_id
                PlivoCXCallLog callLog = null;
                string lookupMethod    = null;

                if (!string.IsNullOrEmpty(payload.LogId) && int.TryParse(payload.LogId, out int logId))
                {
                    callLog     = await _repository.GetCallLogByIdAsync(logId);
                    lookupMethod = $"log_id={logId}";
                }
                if (callLog == null && !string.IsNullOrEmpty(payload.ConversationId))
                {
                    callLog     = await _repository.GetCallLogByConversationIdAsync(payload.ConversationId);
                    lookupMethod = $"conversation_id={payload.ConversationId}";
                }
                if (callLog == null && !string.IsNullOrEmpty(payload.ConversationId))
                {
                    callLog     = await _repository.GetCallLogByRequestIdAsync(payload.ConversationId);
                    lookupMethod = $"request_id={payload.ConversationId}";
                }

                if (callLog == null)
                {
                    await Cp(CheckpointNames.HangupLogNotFound,
                        $"No call log matched — tried: {lookupMethod}",
                        convId: payload.ConversationId);
                    return Ok();
                }

                await Cp(CheckpointNames.HangupLogFound,
                    $"Call log #{callLog.Id} found via {lookupMethod}",
                    convId: payload.ConversationId,
                    callLogId: callLog.Id,
                    extra: $"phone: {callLog.RecipientPhone}, status_was: {callLog.CallStatus}");

                // ── Step 6: Apply call outcome fields ────────────────────────────────
                callLog.PlivoCXConversationId = payload.ConversationId ?? callLog.PlivoCXConversationId;
                callLog.CallStatus            = payload.CallStatus     ?? callLog.CallStatus;
                callLog.HangupCause           = payload.HangupCause;
                callLog.CallTranscript        = payload.Transcript;
                callLog.ConversationSummary   = payload.Summary;
                callLog.BookedInterviewDate   = payload.InterviewDate;
                callLog.BookedInterviewTime   = payload.InterviewTime;
                callLog.CallEndedAt           = DateTime.UtcNow;
                callLog.UpdatedAt             = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(payload.Duration) &&
                    int.TryParse(payload.Duration, out int dur))
                    callLog.DurationSeconds = dur;

                // ── Step 7: Attempt booking if candidate confirmed ────────────────────
                bool candidateConfirmed = string.Equals(
                    payload.CandidateConfirmed, "true",
                    StringComparison.OrdinalIgnoreCase);

                if (candidateConfirmed &&
                    !string.IsNullOrWhiteSpace(payload.InterviewDate) &&
                    !string.IsNullOrWhiteSpace(payload.InterviewTime))
                {
                    await Cp(CheckpointNames.HangupBookingStarted,
                        $"Booking — date: {payload.InterviewDate}, time: {payload.InterviewTime}",
                        convId: payload.ConversationId, callLogId: callLog.Id);

                    await ProcessBookingAsync(callLog, payload);

                    await Cp(CheckpointNames.HangupBookingDone,
                        $"Booking result — success: {callLog.BookingApiSuccess}, response: {callLog.BookingApiResponse?.Substring(0, Math.Min(200, callLog.BookingApiResponse?.Length ?? 0))}",
                        convId: payload.ConversationId, callLogId: callLog.Id);
                }
                else
                {
                    await Cp(CheckpointNames.HangupBookingSkipped,
                        $"Booking skipped — confirmed: {payload.CandidateConfirmed}, date: {payload.InterviewDate}, time: {payload.InterviewTime}",
                        convId: payload.ConversationId, callLogId: callLog.Id);
                }

                await _repository.UpdateCallLogAsync(callLog);

                await Cp(CheckpointNames.HangupComplete,
                    $"Call log #{callLog.Id} updated — final status: {callLog.CallStatus}",
                    convId: payload.ConversationId, callLogId: callLog.Id);

                return Ok();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                await Cp("hangup_exception", $"Unhandled exception: {ex.Message}",
                    convId: payload?.ConversationId,
                    extra: ex.ToString());
                return Ok(); // Always 200 to Plivo CX
            }
        }

        /// <summary>
        /// Optional: Plivo CX can also POST a recording + transcript here when the recording is ready.
        /// Configure as "Recording URL" in Events tab.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> RecordingCallback()
        {
            // Read raw body — Plivo CX posts form-encoded, not JSON
            string rawBody = string.Empty;
            try
            {
                Request.EnableBuffering();
                using var reader = new System.IO.StreamReader(
                    Request.Body, System.Text.Encoding.UTF8, leaveOpen: true);
                rawBody = await reader.ReadToEndAsync();
            }
            catch { }

            try
            {
                await _repository.SaveWebhookDebugLogAsync(new PlivoCXWebhookDebugLog
                {
                    LogType          = "recording-callback",
                    RawPayload       = rawBody,
                    ProcessingStatus = "received"
                });
                await _logger.LogMessage(LogLevelInfo.Info, $"[PlivoCX] Recording callback: {rawBody}");
            }
            catch { }
            return Ok();
        }

        // ─── Booking logic ────────────────────────────────────────────────────────

        private async Task ProcessBookingAsync(PlivoCXCallLog callLog, PlivoCXHangupPayload payload)
        {
            try
            {
                var irfIdStr       = payload.IrfId ?? callLog.IRFId?.ToString();
                var candidateEmail = payload.CandidateEmail ?? callLog.RecipientEmail;

                if (string.IsNullOrEmpty(candidateEmail) ||
                    string.IsNullOrEmpty(irfIdStr) ||
                    !int.TryParse(irfIdStr, out int irfId))
                {
                    callLog.BookingApiResponse = "Missing irfId or candidateEmail — booking skipped.";
                    return;
                }

                // Parse and normalise date+time
                var dateTimeString = $"{payload.InterviewDate} {payload.InterviewTime}";
                string[] formats   =
                {
                    "yyyy-MM-dd hh:mm tt", "yyyy-MM-dd h:mm tt",
                    "yyyy-MM-dd HH:mm",    "yyyy-MM-dd H:mm",
                    "dd/MM/yyyy hh:mm tt", "dd/MM/yyyy HH:mm",
                    "tomorrow hh:mm tt",   "tomorrow h:mm tt"
                };

                // Resolve "tomorrow" / "day after tomorrow" to actual dates
                var dateLower = payload.InterviewDate?.ToLower().Trim() ?? "";
                if (dateLower == "tomorrow")
                    dateTimeString = $"{DateTime.Now.AddDays(1):yyyy-MM-dd} {payload.InterviewTime}";
                else if (dateLower is "day after tomorrow" or "dayaftertomorrow")
                    dateTimeString = $"{DateTime.Now.AddDays(2):yyyy-MM-dd} {payload.InterviewTime}";

                DateTime? parsedDateTime = null;
                foreach (var fmt in formats)
                {
                    if (DateTime.TryParseExact(dateTimeString, fmt,
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out var dt))
                    {
                        parsedDateTime = dt;
                        break;
                    }
                }

                if (!parsedDateTime.HasValue)
                {
                    // Fallback: try general parse
                    if (DateTime.TryParse(dateTimeString, out var dtFallback))
                        parsedDateTime = dtFallback;
                    else
                    {
                        callLog.BookingApiResponse = $"Could not parse datetime: {dateTimeString}";
                        return;
                    }
                }

                callLog.BookedInterviewDateTime = parsedDateTime.Value;
                callLog.InterviewBooked         = true;

                // Guard: don't double-book
                var hasConfirmed = await _repository.HasConfirmedInterviewAsync(candidateEmail, irfId);
                if (hasConfirmed)
                {
                    callLog.BookingApiResponse = "Already has a confirmed interview — skipped duplicate booking.";
                    callLog.BookingApiSuccess  = false;
                    return;
                }

                await Task.Delay(2000); // brief wait for DB consistency

                var candidateDetails = await _repository.GetCandidateDetailsForScheduleAsync(irfId, candidateEmail);
                if (candidateDetails == null)
                {
                    callLog.BookingApiResponse = "Candidate details not found in DB.";
                    return;
                }

                var dto = new ScheduleCandidatesRequest
                {
                    skillChannelId = candidateDetails.IRFId,
                    requestedBy    = candidateDetails.ClientHRAttemptContact
                };
                var roundedDate = parsedDateTime.Value.RoundToNearSlot(candidateDetails.duration);
                dto.CandidateList.Add(new ScheduleCandidateList
                {
                    EmailId       = candidateEmail,
                    Interviewdate = roundedDate,
                    ResumeLink    = candidateDetails.ResumeLink,
                    Phone         = candidateDetails.Phone,
                    Name          = candidateDetails.CandidateName
                });

                var restClient  = new RestClient("https://api.portal.risebird.io/api/Candidates/Schedule");
                var restRequest = new RestRequest("https://api.portal.risebird.io/api/Candidates/Schedule", Method.Post);
                restRequest.AddHeader("content-type", "application/json");
                restRequest.AddHeader("x-functions-key", "SJJvpXEzy-1fOQN2mygBaTT6gXctL406VhpMwn0BqhKUAzFuBDDP5Q==");
                restRequest.AddHeader("tenant-id", candidateDetails.tenantId);
                restRequest.AddParameter("application/json", JsonConvert.SerializeObject(dto), ParameterType.RequestBody);

                var apiResponse = await restClient.ExecuteAsync(restRequest);

                callLog.BookingApiResponse = apiResponse.Content;
                callLog.BookingApiSuccess  = apiResponse.IsSuccessful;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                callLog.BookingApiResponse = $"Exception: {ex.Message}";
            }
        }

        // ─── Management / test endpoints ─────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetCallLogs(int count = 50)
        {
            var logs = await _repository.GetRecentCallLogsAsync(count);
            return Json(logs);
        }

        [HttpGet]
        public async Task<IActionResult> GetCallLog(int id)
        {
            var log = await _repository.GetCallLogByIdAsync(id);
            return log != null ? Json(log) : NotFound();
        }

        /// <summary>
        /// View recent trigger + hangup debug logs (raw payloads).
        /// Access: /PlivoCXInterviewCall/DebugLogs?count=50
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
        /// View recent granular processing checkpoints for HangupCallback.
        /// Access: /PlivoCXInterviewCall/Checkpoints?count=100
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

        /// <summary>
        /// Quick health-check endpoint — verifies the Plivo CX trigger URL is configured
        /// </summary>
        [HttpGet]
        public IActionResult WebhookTest()
        {
            var triggerUrl = _configuration["PlivoCX:TriggerEndpointUrl"];
            return Json(new
            {
                status     = "ok",
                triggerUrl = string.IsNullOrEmpty(triggerUrl)
                    ? "⚠ NOT SET — add PlivoCX:TriggerEndpointUrl to appsettings.json"
                    : $"✓ configured ({triggerUrl.Substring(0, Math.Min(60, triggerUrl.Length))}...)",
                hangupUrl  = $"{Request.Scheme}://{Request.Host}/PlivoCXInterviewCall/HangupCallback",
                instructions = new[]
                {
                    "1. Go to cx.plivo.com → AI Studio → your interview flow",
                    "2. Click the Trigger node → copy the API URL → paste into appsettings.json PlivoCX:TriggerEndpointUrl",
                    "3. Click Agent Config → Events → set Hangup URL to the hangupUrl above",
                    "4. Make sure the AI Conversation node extracts: interview_date, interview_time, candidate_confirmed"
                }
            });
        }
    }
}
