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
                    PhoneNumber           = recipientPhone,
                    CandidateName         = recipientName ?? "Candidate",
                    CandidateEmail        = recipientEmail,
                    IrfId                 = irfId?.ToString(),
                    JobTitle              = jobTitle,
                    ClientName            = clientName,
                    LogId                 = logId.ToString(),
                    TodayDate             = DateTime.Now.ToString("dddd, dd MMMM yyyy"),
                    TomorrowDate          = DateTime.Now.AddDays(1).ToString("dddd, dd MMMM yyyy"),
                    DayAfterTomorrowDate  = DateTime.Now.AddDays(2).ToString("dddd, dd MMMM yyyy")
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
            await SaveCpAsync(CheckpointNames.HangupReceived, $"HangupCallback hit — Content-Type: {Request.ContentType}");

            // ── Step 1: Read raw body ────────────────────────────────────────────────
            string rawBody = string.Empty;
            try
            {
                Request.EnableBuffering();
                using var reader = new System.IO.StreamReader(
                    Request.Body, System.Text.Encoding.UTF8, leaveOpen: true);
                rawBody = await reader.ReadToEndAsync();
                Request.Body.Position = 0;
                await SaveCpAsync(CheckpointNames.HangupBodyRead,
                    $"Raw body read — length: {rawBody.Length}",
                    extra: rawBody.Length > 500 ? rawBody.Substring(0, 500) + "…" : rawBody);
            }
            catch (Exception ex)
            {
                await SaveCpAsync(CheckpointNames.HangupBodyRead, $"Body read FAILED: {ex.Message}");
            }

            // ── Step 2: Parse — Plivo CX sends a NESTED JSON envelope ───────────────
            //
            //  {
            //    "created_at": "...",
            //    "data": {
            //      "object": {
            //        "conversation_id": "1651-...",
            //        "call_uuid": "...",
            //        "event_data": {
            //          "Initiate Interview Call.call_status": "completed",
            //          "AI Conversation.interview_date":      "tomorrow",
            //          "API Request.log_id":                  "6",
            //          ...
            //        }
            //      }
            //    }
            //  }
            //
            // We parse the envelope, then flatten event_data into PlivoCXHangupPayload
            // by stripping the "NodeName." prefix from each key.

            PlivoCXHangupPayload payload = null;

            try
            {
                var envelope = JsonConvert.DeserializeObject<PlivoCXHangupEvent>(rawBody);
                var obj      = envelope?.Data?.Object;

                if (obj != null)
                {
                    var ed = obj.EventData ?? new System.Collections.Generic.Dictionary<string, string>();

                    payload = new PlivoCXHangupPayload
                    {
                        ConversationId         = obj.ConversationId,
                        CallUUID               = obj.CallUuid,
                        CallStatus             = GetEventDataValue(ed, "call_status"),
                        Duration               = GetEventDataValue(ed, "duration"),
                        HangupCause            = GetEventDataValue(ed, "hangup_cause"),
                        Transcript             = GetEventDataValue(ed, "transcript"),
                        Summary                = GetEventDataValue(ed, "summary"),
                        PhoneNumber            = GetEventDataValue(ed, "phone_number"),
                        CandidateName          = GetEventDataValue(ed, "candidate_name"),
                        CandidateEmail         = GetEventDataValue(ed, "candidate_email"),
                        IrfId                  = GetEventDataValue(ed, "irf_id"),
                        JobTitle               = GetEventDataValue(ed, "job_title"),
                        ClientName             = GetEventDataValue(ed, "client_name"),
                        LogId                  = GetEventDataValue(ed, "log_id"),
                        InterviewDate          = GetEventDataValue(ed, "interview_date"),
                        InterviewTime          = GetEventDataValue(ed, "interview_time"),
                        CandidateConfirmed     = GetEventDataValue(ed, "candidate_confirmed"),
                        ConfirmedCandidateName = GetEventDataValue(ed, "confirmed_candidate_name")
                    };

                    await SaveCpAsync(CheckpointNames.HangupJsonParsed,
                        $"Parsed nested envelope — conv_id: {payload.ConversationId}, log_id: {payload.LogId}, " +
                        $"call_status: {payload.CallStatus}, interview_date: {payload.InterviewDate}, " +
                        $"interview_time: {payload.InterviewTime}, confirmed: {payload.CandidateConfirmed}",
                        convId: payload.ConversationId,
                        extra: $"event_data keys: {string.Join(", ", ed.Keys)}");
                }
            }
            catch (Exception ex)
            {
                await SaveCpAsync(CheckpointNames.HangupParseFailed, $"Nested JSON parse FAILED: {ex.Message}");
            }

            // ── Fallback: flat form-urlencoded (keep for safety) ─────────────────────
            if (payload == null ||
                (string.IsNullOrEmpty(payload.ConversationId) && string.IsNullOrEmpty(payload.LogId)))
            {
                if (!Request.ContentType?.Contains("application/json") == true)
                {
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
                        await SaveCpAsync(CheckpointNames.HangupFormParsed,
                            $"Parsed as form-encoded — conv_id: {payload.ConversationId}, log_id: {payload.LogId}",
                            convId: payload.ConversationId,
                            extra: $"keys: {string.Join(", ", form.Keys)}");
                    }
                    catch (Exception ex)
                    {
                        await SaveCpAsync(CheckpointNames.HangupParseFailed, $"Form parse FAILED: {ex.Message}");
                    }
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
                await SaveCpAsync(CheckpointNames.HangupParseFailed,
                    "No conv_id or log_id after all parse attempts — dropping webhook",
                    extra: $"raw (first 300): {rawBody?.Substring(0, Math.Min(300, rawBody?.Length ?? 0))}");
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
                    callLog      = await _repository.GetCallLogByIdAsync(logId);
                    lookupMethod = $"log_id={logId}";
                }
                if (callLog == null && !string.IsNullOrEmpty(payload.ConversationId))
                {
                    callLog      = await _repository.GetCallLogByConversationIdAsync(payload.ConversationId);
                    lookupMethod = $"conversation_id={payload.ConversationId}";
                }
                if (callLog == null && !string.IsNullOrEmpty(payload.ConversationId))
                {
                    callLog      = await _repository.GetCallLogByRequestIdAsync(payload.ConversationId);
                    lookupMethod = $"request_id={payload.ConversationId}";
                }

                if (callLog == null)
                {
                    await SaveCpAsync(CheckpointNames.HangupLogNotFound,
                        $"No call log matched — tried: {lookupMethod}",
                        convId: payload.ConversationId);
                    return Ok();
                }

                await SaveCpAsync(CheckpointNames.HangupLogFound,
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
                callLog.BookedInterviewDate   = ResolveRelativeDate(payload.InterviewDate);
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
                    await SaveCpAsync(CheckpointNames.HangupBookingStarted,
                        $"Booking — date: {payload.InterviewDate}, time: {payload.InterviewTime}",
                        convId: payload.ConversationId, callLogId: callLog.Id);

                    await ProcessBookingAsync(callLog, payload, payload.ConversationId);

                    await SaveCpAsync(CheckpointNames.HangupBookingDone,
                        $"Booking result — success: {callLog.BookingApiSuccess}, response: {callLog.BookingApiResponse?.Substring(0, Math.Min(200, callLog.BookingApiResponse?.Length ?? 0))}",
                        convId: payload.ConversationId, callLogId: callLog.Id);
                }
                else
                {
                    await SaveCpAsync(CheckpointNames.HangupBookingSkipped,
                        $"Booking skipped — confirmed: {payload.CandidateConfirmed}, date: {payload.InterviewDate}, time: {payload.InterviewTime}",
                        convId: payload.ConversationId, callLogId: callLog.Id);
                }

                await _repository.UpdateCallLogAsync(callLog);

                await SaveCpAsync(CheckpointNames.HangupComplete,
                    $"Call log #{callLog.Id} updated — final status: {callLog.CallStatus}",
                    convId: payload.ConversationId, callLogId: callLog.Id);

                return Ok();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                await SaveCpAsync("hangup_exception", $"Unhandled exception: {ex.Message}",
                    convId: payload?.ConversationId,
                    extra: ex.ToString());
                return Ok(); // Always 200 to Plivo CX
            }
        }

        /// <summary>
        /// Looks up a variable in the Plivo CX event_data dictionary.
        /// Keys are "NodeName.variable_name" — we match by the variable_name suffix,
        /// falling back to an exact match if needed.
        /// </summary>
        private static string GetEventDataValue(
            System.Collections.Generic.Dictionary<string, string> eventData, string key)
        {
            if (eventData == null || string.IsNullOrEmpty(key)) return null;

            // Exact match first
            if (eventData.TryGetValue(key, out var exact)) return exact;

            // Suffix match: "SomeNode.key" → key
            var suffix = "." + key;
            foreach (var kvp in eventData)
            {
                if (kvp.Key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }

            return null;
        }

        /// <summary>
        /// Converts relative date words to a real date string (yyyy-MM-dd).
        /// If the AI returns an actual date, it passes through unchanged.
        /// </summary>
        private static string ResolveRelativeDate(string rawDate)
        {
            if (string.IsNullOrWhiteSpace(rawDate)) return rawDate;

            switch (rawDate.ToLowerInvariant().Trim())
            {
                case "tomorrow":
                    return DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
                case "day after tomorrow":
                case "dayaftertomorrow":
                    return DateTime.Now.AddDays(2).ToString("yyyy-MM-dd");
                default:
                    return rawDate; // already a real date string
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

        private async Task ProcessBookingAsync(
            PlivoCXCallLog callLog, PlivoCXHangupPayload payload, string convId = null)
        {
            try
            {
                var irfIdStr = payload.IrfId ?? callLog.IRFId?.ToString();

                // ── Resolve candidate email ──────────────────────────────────────────
                // Priority: payload → call log → DB lookup by CandidateId (same pattern as Vapi)
                var candidateEmail = payload.CandidateEmail ?? callLog.RecipientEmail;
                if (string.IsNullOrEmpty(candidateEmail) && callLog.CandidateId.HasValue)
                {
                    candidateEmail = await _repository.GetCandidateEmailByIdAsync(callLog.CandidateId.Value);
                    await SaveCpAsync("booking_email_from_db",
                        $"Email resolved from CandidateId {callLog.CandidateId}: {candidateEmail}",
                        convId: convId, callLogId: callLog.Id);
                }

                if (string.IsNullOrEmpty(candidateEmail) ||
                    string.IsNullOrEmpty(irfIdStr) ||
                    !int.TryParse(irfIdStr, out int irfId))
                {
                    var missing = string.IsNullOrEmpty(candidateEmail) ? "candidateEmail" : "irfId";
                    callLog.BookingApiResponse = $"Missing {missing} — booking skipped.";
                    await SaveCpAsync("booking_missing_data",
                        callLog.BookingApiResponse, convId: convId, callLogId: callLog.Id);
                    return;
                }

                // ── Parse and normalise date+time ────────────────────────────────────
                // interview_date should already be a real date (e.g. "2026-03-07") thanks to
                // the AI instructions using {{tomorrow_date}}. ResolveRelativeDate handles any
                // edge case where the AI still returns a relative word.
                var resolvedDate   = ResolveRelativeDate(payload.InterviewDate);
                var dateTimeString = $"{resolvedDate} {payload.InterviewTime}";

                string[] formats =
                {
                    // ISO date (what the AI should return after our instruction fix)
                    "yyyy-MM-dd hh:mm tt",  "yyyy-MM-dd h:mm tt",
                    "yyyy-MM-dd HH:mm",     "yyyy-MM-dd H:mm",
                    // Full long date — "Saturday, 07 March 2026 10:00 AM"
                    // (what the AI returns when it echoes back {{tomorrow_date}})
                    "dddd, dd MMMM yyyy hh:mm tt", "dddd, dd MMMM yyyy h:mm tt",
                    "dddd, dd MMMM yyyy HH:mm",    "dddd, dd MMMM yyyy H:mm",
                    // Short date variants
                    "dd MMMM yyyy hh:mm tt", "dd MMMM yyyy h:mm tt",
                    "dd MMMM yyyy HH:mm",    "dd MMMM yyyy H:mm",
                    "MMMM dd yyyy hh:mm tt", "MMMM dd yyyy HH:mm",
                    "dd/MM/yyyy hh:mm tt",   "dd/MM/yyyy HH:mm"
                };

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
                // Last-resort: let the runtime try (handles locale-specific variations)
                if (!parsedDateTime.HasValue && DateTime.TryParse(dateTimeString, out var dtFallback))
                    parsedDateTime = dtFallback;

                if (!parsedDateTime.HasValue)
                {
                    callLog.BookingApiResponse = $"Could not parse datetime: '{dateTimeString}'";
                    await SaveCpAsync("booking_datetime_parse_failed",
                        callLog.BookingApiResponse, convId: convId, callLogId: callLog.Id,
                        extra: $"raw date: {payload.InterviewDate}, raw time: {payload.InterviewTime}");
                    return;
                }

                // Normalise BookedInterviewDate to yyyy-MM-dd regardless of what
                // the AI returned (e.g. "Saturday, 07 March 2026" → "2026-03-07")
                callLog.BookedInterviewDate     = parsedDateTime.Value.ToString("yyyy-MM-dd");
                callLog.BookedInterviewDateTime = parsedDateTime.Value;
                callLog.InterviewBooked         = true;

                await SaveCpAsync("booking_datetime_parsed",
                    $"Parsed datetime: {parsedDateTime.Value:yyyy-MM-dd HH:mm} from '{dateTimeString}' — stored date: {callLog.BookedInterviewDate}",
                    convId: convId, callLogId: callLog.Id);

                // ── Guard: don't double-book ─────────────────────────────────────────
                var hasConfirmed = await _repository.HasConfirmedInterviewAsync(candidateEmail, irfId);
                if (hasConfirmed)
                {
                    callLog.BookingApiResponse = "Already has a confirmed interview — skipped duplicate booking.";
                    callLog.BookingApiSuccess  = false;
                    await SaveCpAsync("booking_duplicate_skipped",
                        callLog.BookingApiResponse, convId: convId, callLogId: callLog.Id);
                    return;
                }

                await Task.Delay(2000); // brief wait for DB consistency

                // ── Fetch full candidate details (same SP as Vapi) ───────────────────
                var candidateDetails = await _repository.GetCandidateDetailsForScheduleAsync(irfId, candidateEmail);
                if (candidateDetails == null)
                {
                    callLog.BookingApiResponse = $"Candidate details not found in DB — irfId: {irfId}, email: {candidateEmail}";
                    await SaveCpAsync("booking_candidate_not_found",
                        callLog.BookingApiResponse, convId: convId, callLogId: callLog.Id);
                    return;
                }

                await SaveCpAsync("booking_candidate_found",
                    $"Candidate: {candidateDetails.CandidateName}, tenantId: {candidateDetails.tenantId}, duration: {candidateDetails.duration}",
                    convId: convId, callLogId: callLog.Id);

                // ── Build Schedule API request ────────────────────────────────────────
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

                await SaveCpAsync("booking_api_request",
                    $"Calling Schedule API — slot: {roundedDate:yyyy-MM-dd HH:mm}, tenant: {candidateDetails.tenantId}",
                    convId: convId, callLogId: callLog.Id,
                    extra: JsonConvert.SerializeObject(dto));

                // ── Call the Schedule API ─────────────────────────────────────────────
                const string apiUrl = "https://api.portal.risebird.io/api/Candidates/Schedule";
                var restClient  = new RestClient(apiUrl);
                var restRequest = new RestRequest(apiUrl, Method.Post);
                restRequest.AddHeader("content-type", "application/json");
                restRequest.AddHeader("x-functions-key", "SJJvpXEzy-1fOQN2mygBaTT6gXctL406VhpMwn0BqhKUAzFuBDDP5Q==");
                restRequest.AddHeader("tenant-id", candidateDetails.tenantId);
                restRequest.AddParameter("application/json", JsonConvert.SerializeObject(dto), ParameterType.RequestBody);

                var apiResponse = await restClient.ExecuteAsync(restRequest);

                callLog.BookingApiResponse = apiResponse.Content;
                callLog.BookingApiSuccess  = apiResponse.IsSuccessful;

                await SaveCpAsync("booking_api_response",
                    $"Schedule API — status: {apiResponse.StatusCode}, success: {apiResponse.IsSuccessful}",
                    convId: convId, callLogId: callLog.Id,
                    extra: $"response: {apiResponse.Content?.Substring(0, Math.Min(500, apiResponse.Content?.Length ?? 0))}");
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                callLog.BookingApiResponse = $"Exception: {ex.Message}";
                await SaveCpAsync("booking_exception",
                    $"Exception: {ex.Message}", convId: convId, callLogId: callLog.Id,
                    extra: ex.ToString());
            }
        }

        // ─── Shared checkpoint helper ─────────────────────────────────────────────

        private async Task SaveCpAsync(
            string name, string msg, string convId = null, int? callLogId = null, string extra = null)
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
