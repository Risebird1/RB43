using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.Business.PlivoCX;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using Risebird.Web.Extensions;
using Risebird.Web.Services;
using System;
using System.Linq;
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
        private readonly PlivoCXCallOutcomeProcessor _outcomeProcessor;
        private readonly IElevenLabsAgentService _elevenLabs;

        public PlivoCXInterviewCallController(
            IConfiguration              configuration,
            IPlivoCXService             plivoCXService,
            IPlivoCXCallRepository      repository,
            IApplicationErrorLogger     applicationErrorLogger,
            PlivoCXCallOutcomeProcessor outcomeProcessor,
            IElevenLabsAgentService     elevenLabs)
        {
            _configuration    = configuration;
            _plivoCXService   = plivoCXService;
            _repository       = repository;
            _logger           = applicationErrorLogger;
            _outcomeProcessor = outcomeProcessor;
            _elevenLabs       = elevenLabs;
        }

        // ─── Dashboard ───────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index(
            int    page        = 1,
            int    pageSize    = 50,
            string status      = null,
            string purpose     = null,
            string meeting     = null,
            string disposition = null,
            string dateFrom    = null,
            string dateTo      = null,
            string clientName  = null)
        {
            try
            {
                if (page     < 1)   page     = 1;
                if (pageSize < 10)  pageSize = 10;
                if (pageSize > 200) pageSize = 200;

                DateTime? parsedDateFrom = DateTime.TryParse(dateFrom, out var df) ? df : (DateTime?)null;
                DateTime? parsedDateTo   = DateTime.TryParse(dateTo,   out var dt) ? dt : (DateTime?)null;

                var calls = await _repository.GetCallLogDetailsAsync(
                    page, pageSize, status, purpose, meeting, disposition,
                    parsedDateFrom, parsedDateTo, clientName);

                var allClientNames = await _repository.GetAllClientNamesAsync();

                var first = calls.FirstOrDefault();
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
                ViewBag.ClientName  = clientName;
                ViewBag.AllClientNames = allClientNames;
                ViewBag.DateFrom    = parsedDateFrom.HasValue ? parsedDateFrom.Value.ToString("yyyy-MM-dd") : null;
                ViewBag.DateTo      = parsedDateTo.HasValue   ? parsedDateTo.Value.ToString("yyyy-MM-dd")   : null;

                return View(calls);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                ViewBag.Page = 1; ViewBag.PageSize = pageSize; ViewBag.TotalCount = 0; ViewBag.TotalPages = 1;
                ViewBag.Status = status; ViewBag.Purpose = purpose;
                ViewBag.Meeting = meeting; ViewBag.Disposition = disposition;
                ViewBag.ClientName = clientName;
                ViewBag.AllClientNames = new System.Collections.Generic.List<string>();
                ViewBag.DateFrom = dateFrom; ViewBag.DateTo = dateTo;
                return View(new System.Collections.Generic.List<Risebird.Entities.ViewModel.PlivoCXCallLogDetailVM>());
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

        // ─── Reschedule call trigger ─────────────────────────────────────────────

        /// <summary>
        /// Initiate a reschedule call for a candidate whose previous interview was missed or cancelled.
        /// Uses the separate Plivo CX reschedule agent (PlivoCX:RescheduleTriggerEndpointUrl).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiateRescheduleCall(
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

                var callLog = new PlivoCXCallLog
                {
                    RecipientName    = recipientName,
                    RecipientPhone   = recipientPhone,
                    RecipientEmail   = recipientEmail,
                    IRFId            = irfId,
                    CandidateId      = candidateId,
                    JobTitle         = jobTitle,
                    ClientName       = clientName,
                    CallStatus       = "initiated",
                    InterviewBooked  = false,
                    CallPurpose      = "reschedule",
                    TriggeredBy      = userEmail
                };
                int logId = await _repository.SaveCallLogAsync(callLog);
                callLog.Id = logId;

                var triggerRequest = new PlivoCXTriggerRequest
                {
                    PhoneNumber           = recipientPhone,
                    CandidateName         = recipientName ?? "Candidate",
                    CandidateEmail        = recipientEmail,
                    IrfId                 = irfId?.ToString(),
                    JobTitle              = jobTitle,
                    ClientName            = clientName,
                    LogId                 = logId.ToString(),
                    CallPurpose           = "reschedule",
                    OriginalInterviewDate = originalInterviewDate,
                    TodayDate             = DateTime.Now.ToString("dddd, dd MMMM yyyy"),
                    TomorrowDate          = DateTime.Now.AddDays(1).ToString("dddd, dd MMMM yyyy"),
                    DayAfterTomorrowDate  = DateTime.Now.AddDays(2).ToString("dddd, dd MMMM yyyy")
                };

                var response = await _plivoCXService.TriggerRescheduleCallAsync(triggerRequest);

                await _repository.SaveWebhookDebugLogAsync(new PlivoCXWebhookDebugLog
                {
                    LogType          = "reschedule-trigger-request",
                    HttpStatusCode   = response?.HttpStatusCode,
                    PlivoCXId        = response?.ApiId,
                    CallLogId        = logId,
                    RawPayload       = JsonConvert.SerializeObject(triggerRequest),
                    ProcessingStatus = string.IsNullOrEmpty(response?.Error) ? "success" : "failed",
                    ErrorMessage     = response?.Error
                });
                await _repository.SaveWebhookDebugLogAsync(new PlivoCXWebhookDebugLog
                {
                    LogType          = "reschedule-trigger-response",
                    HttpStatusCode   = response?.HttpStatusCode,
                    PlivoCXId        = response?.ApiId,
                    CallLogId        = logId,
                    RawPayload       = response?.RawResponseBody,
                    ProcessingStatus = string.IsNullOrEmpty(response?.Error) ? "success" : "failed",
                    ErrorMessage     = response?.Error
                });

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

                    TempData["Error"] = $"Reschedule trigger failed: {response.Error}";
                    return RedirectToAction("Index");
                }

                TempData["Success"] = $"Reschedule call initiated for {recipientName} ({recipientPhone}).";
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
                        ConfirmedCandidateName = GetEventDataValue(ed, "confirmed_candidate_name"),
                        CandidateDisposition   = GetEventDataValue(ed, "candidate_disposition")
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
                        ConfirmedCandidateName = form["confirmed_candidate_name"],
                        CandidateDisposition   = form["candidate_disposition"]
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

            // ── Steps 5-7: find the call log, apply the outcome, book, advance the campaign ─
            // Shared with the ElevenLabs post-call webhook (PlivoCXCallOutcomeProcessor).
            await _outcomeProcessor.ProcessAsync(payload);
            return Ok(); // Always 200 to Plivo CX
        }

        /// <summary>
        /// ElevenLabs post-call webhook for PlivoCX campaigns run on ElevenLabs. Point a workspace's
        /// post-call webhook here when campaign calls run on their own ElevenLabs workspace
        /// (ElevenLabs:Campaigns:ApiKey). On a workspace shared with the WhatsApp phone screener,
        /// PhoneScreener/ElevenLabsWebhook receives the event and hands campaign calls on the same way.
        /// Signed with ElevenLabs-Signature; always answers 200 once the signature checks out.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ElevenLabsCallback()
        {
            string rawBody;
            using (var reader = new System.IO.StreamReader(Request.Body, System.Text.Encoding.UTF8))
                rawBody = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(rawBody)) return Ok();

            var signature = Request.Headers.TryGetValue("ElevenLabs-Signature", out var sig) ? sig.ToString() : null;
            if (!_elevenLabs.VerifyWebhookSignature(rawBody, signature))
                return StatusCode(401, "Invalid signature.");

            try
            {
                await _repository.SaveWebhookDebugLogAsync(new PlivoCXWebhookDebugLog
                {
                    LogType          = "elevenlabs-postcall",
                    RawPayload       = rawBody.Length > 20000 ? rawBody.Substring(0, 20000) : rawBody,
                    ProcessingStatus = "received"
                });
            }
            catch { }

            Newtonsoft.Json.Linq.JObject root;
            try { root = Newtonsoft.Json.Linq.JObject.Parse(rawBody); }
            catch { return Ok(); }

            var payload = ElevenLabsCampaignWebhook.Map((string)root["type"], root["data"] as Newtonsoft.Json.Linq.JObject);
            if (payload != null)
                await _outcomeProcessor.ProcessAsync(payload);
            return Ok();
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
