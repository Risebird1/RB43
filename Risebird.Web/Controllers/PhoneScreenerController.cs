using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// AI Phone Screener. Places Plivo CX conversational-voice pre-screening calls to
    /// candidates in a skill channel (IRF) and surfaces the outcome (JSON output,
    /// transcript, recording URL, disposition, etc.) on provider-agnostic screens.
    ///
    /// Each skill channel maps to its own Plivo assistant, addressed by the flow
    /// trigger URL stored on IRF.PlivoAssistantUrl.
    /// </summary>
    [Authorize]
    public class PhoneScreenerController : Controller
    {
        private readonly IPhoneScreenRepository _phoneScreenRepository;
        private readonly IIRFService _irfService;
        private readonly IPlivoCXService _plivoCXService;
        private readonly IPlivoWhatsAppService _plivoWhatsAppService;
        private readonly IElevenLabsAgentService _elevenLabsAgentService;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        private static readonly string[] AllowedRoles =
        {
            "Admin", "HR", "SeniorHR", "SupportAdmin",
            "InterviewerManagement", "SeniorInterviewerManagement"
        };

        public PhoneScreenerController(
            IPhoneScreenRepository phoneScreenRepository,
            IIRFService irfService,
            IPlivoCXService plivoCXService,
            IPlivoWhatsAppService plivoWhatsAppService,
            IElevenLabsAgentService elevenLabsAgentService,
            IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _phoneScreenRepository = phoneScreenRepository;
            _irfService = irfService;
            _plivoCXService = plivoCXService;
            _plivoWhatsAppService = plivoWhatsAppService;
            _elevenLabsAgentService = elevenLabsAgentService;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
        }

        // ────────────────────────────────────────────────────────────────────
        // Screen A — Campaign Outcomes
        // ────────────────────────────────────────────────────────────────────

        public async Task<IActionResult> Outcomes(int id, string irfName)
        {
            if (!IsAuthorized(out _))
                return RedirectToAction("SignOut", "Session");

            try
            {
                var emailId = GetSessionEmail();
                var header = await _phoneScreenRepository.GetIRFHeaderAsync(id);
                var candidates = (await _irfService.GetIRFCandidates(id, emailId))?.ToList()
                                 ?? new List<Candidate>();
                var calls = (await _phoneScreenRepository.GetCallsByIRFAsync(id))?.ToList()
                            ?? new List<PhoneScreenCall>();

                // Latest call per candidate (calls are ordered by Id DESC).
                var latestByCandidate = calls
                    .Where(c => c.CandidateId.HasValue)
                    .GroupBy(c => c.CandidateId.Value)
                    .ToDictionary(g => g.Key, g => g.First());

                var vm = new PhoneScreenOutcomesVM
                {
                    IRFId = id,
                    IRFCode = header?.IRFCode,
                    IRFName = header?.IRFName ?? irfName,
                    ClientName = header?.ClientName,
                    PlivoAssistantUrl = header?.PlivoAssistantUrl,
                    HasAssistantConfigured = !string.IsNullOrWhiteSpace(header?.PlivoAssistantUrl),
                    PlivoWhatsAppAssistantUrl = header?.PlivoWhatsAppAssistantUrl,
                    HasWhatsAppAssistantConfigured = !string.IsNullOrWhiteSpace(header?.PlivoWhatsAppAssistantUrl),
                    ElevenLabsAgentId = header?.ElevenLabsAgentId,
                    HasElevenLabsWhatsAppConfigured = !string.IsNullOrWhiteSpace(header?.ElevenLabsAgentId),
                    LaunchDate = header?.StartDate
                };

                foreach (var cand in candidates)
                {
                    latestByCandidate.TryGetValue(cand.id, out var call);
                    var row = new PhoneScreenOutcomeRowVM
                    {
                        CandidateId = cand.id,
                        IRFId = id,
                        CandidateName = cand.Name,
                        CandidateEmail = cand.EmailId,
                        Phone = cand.Phone
                    };

                    if (call != null)
                    {
                        row.HasCall = true;
                        row.CallId = call.Id;
                        row.Provider = call.Provider;
                        row.Channel = (call.Provider ?? "").IndexOf("whatsapp", StringComparison.OrdinalIgnoreCase) >= 0
                            ? "whatsapp" : "voice";
                        row.RawStatus = call.Status;
                        row.Status = CanonicalStatus(call.Status);
                        row.EndedReason = call.EndedReason;
                        row.Summary = call.Summary;
                        row.RecordingUrl = call.RecordingUrl;
                        row.SuccessEvaluation = NormalizeSuccess(call);
                        row.StartedAt = call.StartedAt;
                        row.DurationSeconds = call.DurationSeconds;
                    }

                    vm.Rows.Add(row);
                }

                ComputeMetrics(vm, calls);
                return View(vm);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index", "Home");
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Screen B — Candidate Call Detail
        // ────────────────────────────────────────────────────────────────────

        public async Task<IActionResult> CallDetail(int id)
        {
            if (!IsAuthorized(out _))
                return RedirectToAction("SignOut", "Session");

            try
            {
                var call = await _phoneScreenRepository.GetCallByIdAsync(id);
                if (call == null)
                    return RedirectToAction("Index", "Home");

                // Best-effort: if the call has ended but the recording/transcript never
                // arrived on the webhook, pull them from Plivo's REST APIs by call UUID.
                call = await TryBackfillMediaAsync(call);

                var header = await _phoneScreenRepository.GetIRFHeaderAsync(call.IRFId);
                var vm = MapToResultVM(call, header);
                return View(vm);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index", "Home");
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Place an outbound screening call (manual, one candidate)
        // ────────────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceCall(int candidateId, int irfId)
        {
            if (!IsAuthorized(out _))
                return Json(new { success = false, message = "Session expired. Please sign in again." });

            try
            {
                var emailId = GetSessionEmail();
                var header = await _phoneScreenRepository.GetIRFHeaderAsync(irfId);

                if (header == null)
                    return Json(new { success = false, message = "Skill channel not found." });

                if (string.IsNullOrWhiteSpace(header.PlivoAssistantUrl))
                    return Json(new
                    {
                        success = false,
                        message = "No Plivo assistant URL is configured for this skill channel. Add it on the skill channel's edit page."
                    });

                var candidates = (await _irfService.GetIRFCandidates(irfId, emailId))?.ToList()
                                 ?? new List<Candidate>();
                var candidate = candidates.FirstOrDefault(c => c.id == candidateId);
                if (candidate == null)
                    return Json(new { success = false, message = "Candidate not found in this skill channel." });

                if (string.IsNullOrWhiteSpace(candidate.Phone))
                    return Json(new { success = false, message = "Candidate has no phone number on file." });

                var callerId = _configuration["PlivoCX:PhoneScreenerCallerId"]
                               ?? _configuration["PlivoCX:AvailabilityCallerId"]
                               ?? _configuration["Plivo:FromNumber"]
                               ?? "+918031136547";
                //candidate.Phone = "+918105232468";
                //candidate.Phone = "+918888571923";

                // Pre-save the call row so the webhook can resolve it via log_id.
                var callRecord = new PhoneScreenCall
                {
                    Provider = "plivo",
                    IRFId = irfId,
                    CandidateId = candidate.id,
                    CandidateMasterId = candidate.CandidateMasterId,
                    CandidateName = candidate.Name,
                    CandidateEmail = candidate.EmailId,
                    Phone = candidate.Phone,
                    Status = "initiated",
                    StartedAt = DateTime.UtcNow,
                    CreatedBy = emailId
                };
                var callId = await _phoneScreenRepository.InsertCallAsync(callRecord);
                if (callId <= 0)
                    return Json(new { success = false, message = "Could not create the call record." });

                var triggerRequest = new PhoneScreenTriggerRequest
                {
                    CallerId = callerId,
                    PhoneNumber = candidate.Phone,
                    CandidateName = candidate.Name ?? "there",
                    CandidateEmail = candidate.EmailId,
                    JobTitle = header.IRFName ?? "the role",
                    LogId = callId.ToString(),
                    TodayDate = DateTime.Now.ToString("dddd, dd MMMM yyyy")
                };

                var response = await _plivoCXService.TriggerByUrlAsync(header.PlivoAssistantUrl, triggerRequest);

                if (response != null && string.IsNullOrEmpty(response.Error))
                {
                    await _phoneScreenRepository.UpdateCallTriggerResultAsync(
                        callId, "triggered", response.ConversationId, null);
                    return Json(new
                    {
                        success = true,
                        callId,
                        status = "triggered",
                        message = "Call placed. The screening conversation is now in progress."
                    });
                }

                await _phoneScreenRepository.UpdateCallTriggerResultAsync(
                    callId, "trigger-failed", response?.ConversationId, null);
                return Json(new
                {
                    success = false,
                    callId,
                    status = "trigger-failed",
                    message = "Plivo did not accept the call trigger: " + (response?.Error ?? "unknown error")
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Failed to place the call. Please try again." });
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Start an outbound WhatsApp screening conversation (manual, one candidate)
        // Driven by an ElevenLabs agent mapped to the skill channel. The approved
        // WhatsApp template opens the conversation; the agent then screens the
        // candidate and posts the outcome to ElevenLabsWebhook.
        // ────────────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceWhatsAppScreen(int candidateId, int irfId)
        {
            if (!IsAuthorized(out _))
                return Json(new { success = false, message = "Session expired. Please sign in again." });

            try
            {
                var emailId = GetSessionEmail();
                var header = await _phoneScreenRepository.GetIRFHeaderAsync(irfId);

                if (header == null)
                    return Json(new { success = false, message = "Skill channel not found." });

                if (string.IsNullOrWhiteSpace(header.ElevenLabsAgentId))
                    return Json(new
                    {
                        success = false,
                        message = "No ElevenLabs WhatsApp agent is configured for this skill channel. Add the agent ID on the skill channel's edit page."
                    });

                var candidates = (await _irfService.GetIRFCandidates(irfId, emailId))?.ToList()
                                 ?? new List<Candidate>();
                var candidate = candidates.FirstOrDefault(c => c.id == candidateId);
                if (candidate == null)
                    return Json(new { success = false, message = "Candidate not found in this skill channel." });

                if (string.IsNullOrWhiteSpace(candidate.Phone))
                    return Json(new { success = false, message = "Candidate has no phone number on file." });
                candidate.Phone = "+918888571923"; 
                // Pre-save the row so the ElevenLabs post-call webhook can resolve it via log_id.
                var callRecord = new PhoneScreenCall
                {
                    Provider = "elevenlabs-whatsapp",
                    IRFId = irfId,
                    CandidateId = candidate.id,
                    CandidateMasterId = candidate.CandidateMasterId,
                    CandidateName = candidate.Name,
                    CandidateEmail = candidate.EmailId,
                    Phone = candidate.Phone,
                    Status = "initiated",
                    StartedAt = DateTime.UtcNow,
                    CreatedBy = emailId
                };
                var callId = await _phoneScreenRepository.InsertCallAsync(callRecord);
                if (callId <= 0)
                    return Json(new { success = false, message = "Could not create the WhatsApp screen record." });

                var candidateName = candidate.Name ?? "there";
                var clientName = header.ClientName ?? "our client";
                var jobTitle = header.IRFName ?? "the role";

                // Dynamic variables are echoed back on the post-call webhook (log_id lets us
                // resolve the row) and are available to the agent prompt at runtime.
                var dynamicVariables = new Dictionary<string, object>
                {
                    ["log_id"] = callId.ToString(),
                    ["candidate_name"] = candidateName,
                    ["candidate_email"] = candidate.EmailId ?? string.Empty,
                    ["candidate_phone"] = candidate.Phone,
                    ["client_name"] = clientName,
                    ["job_title"] = jobTitle,
                    ["today_date"] = DateTime.Now.ToString("dddd, dd MMMM yyyy")
                };

                // Ordered body parameters for the approved template
                // ({{1}} candidate name, {{2}} client name, {{3}} job title).
                var templateBodyParams = new List<string> { candidateName, clientName, jobTitle };

                var response = await _elevenLabsAgentService.SendWhatsAppScreenAsync(
                    header.ElevenLabsAgentId, candidate.Phone, dynamicVariables, templateBodyParams);

                if (response != null && string.IsNullOrEmpty(response.Error) &&
                    !string.IsNullOrWhiteSpace(response.ConversationId))
                {
                    await _phoneScreenRepository.UpdateCallTriggerResultAsync(
                        callId, "triggered", response.ConversationId, null);
                    return Json(new
                    {
                        success = true,
                        callId,
                        status = "triggered",
                        message = "WhatsApp screening started. The candidate has been messaged and the conversation is now in progress."
                    });
                }

                await _phoneScreenRepository.UpdateCallTriggerResultAsync(
                    callId, "trigger-failed", response?.ConversationId, null);
                return Json(new
                {
                    success = false,
                    callId,
                    status = "trigger-failed",
                    message = "ElevenLabs did not accept the WhatsApp message: " + (response?.Error ?? "unknown error")
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Failed to start the WhatsApp screen. Please try again." });
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Start an outbound WhatsApp screening conversation via Plivo CX (manual).
        // Runs alongside the ElevenLabs path (PlaceWhatsAppScreen); this one uses
        // the skill channel's Plivo CX WhatsApp flow trigger URL and posts its
        // outcome to the generic Webhook action.
        // ────────────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlacePlivoWhatsAppScreen(int candidateId, int irfId)
        {
            if (!IsAuthorized(out _))
                return Json(new { success = false, message = "Session expired. Please sign in again." });

            try
            {
                var emailId = GetSessionEmail();
                var header = await _phoneScreenRepository.GetIRFHeaderAsync(irfId);

                if (header == null)
                    return Json(new { success = false, message = "Skill channel not found." });

                if (string.IsNullOrWhiteSpace(header.PlivoWhatsAppAssistantUrl))
                    return Json(new
                    {
                        success = false,
                        message = "No Plivo WhatsApp assistant URL is configured for this skill channel. Add it on the skill channel's edit page."
                    });

                var candidates = (await _irfService.GetIRFCandidates(irfId, emailId))?.ToList()
                                 ?? new List<Candidate>();
                var candidate = candidates.FirstOrDefault(c => c.id == candidateId);
                if (candidate == null)
                    return Json(new { success = false, message = "Candidate not found in this skill channel." });

                if (string.IsNullOrWhiteSpace(candidate.Phone))
                    return Json(new { success = false, message = "Candidate has no phone number on file." });
                candidate.Phone = "+918888571923";
                // Pre-save the row so the WhatsApp agent's webhook can resolve it via log_id.
                var callRecord = new PhoneScreenCall
                {
                    Provider = "plivo-whatsapp",
                    IRFId = irfId,
                    CandidateId = candidate.id,
                    CandidateMasterId = candidate.CandidateMasterId,
                    CandidateName = candidate.Name,
                    CandidateEmail = candidate.EmailId,
                    Phone = candidate.Phone,
                    Status = "initiated",
                    StartedAt = DateTime.UtcNow,
                    CreatedBy = emailId
                };
                var callId = await _phoneScreenRepository.InsertCallAsync(callRecord);
                if (callId <= 0)
                    return Json(new { success = false, message = "Could not create the WhatsApp screen record." });

                var triggerRequest = new PhoneScreenWhatsAppTriggerRequest
                {
                    PhoneNumber = candidate.Phone,
                    CandidatePhone = candidate.Phone,
                    CandidateName = candidate.Name ?? "there",
                    CandidateEmail = candidate.EmailId,
                    JobTitle = header.IRFName ?? "the role",
                    ClientName = header.ClientName,
                    LogId = callId.ToString(),
                    LogIdentifier = callId.ToString(),
                    TodayDate = DateTime.Now.ToString("dddd, dd MMMM yyyy")
                };

                var response = await _plivoWhatsAppService.TriggerByUrlAsync(
                    header.PlivoWhatsAppAssistantUrl, triggerRequest);

                if (response != null && string.IsNullOrEmpty(response.Error))
                {
                    await _phoneScreenRepository.UpdateCallTriggerResultAsync(
                        callId, "triggered", response.ConversationId, null);
                    return Json(new
                    {
                        success = true,
                        callId,
                        status = "triggered",
                        message = "WhatsApp screening started (Plivo). The conversation is now in progress."
                    });
                }

                await _phoneScreenRepository.UpdateCallTriggerResultAsync(
                    callId, "trigger-failed", response?.ConversationId, null);
                return Json(new
                {
                    success = false,
                    callId,
                    status = "trigger-failed",
                    message = "Plivo did not accept the WhatsApp trigger: " + (response?.Error ?? "unknown error")
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Failed to start the Plivo WhatsApp screen. Please try again." });
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Lightweight status poll (used by the Outcomes page for live updates)
        // ────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> CallStatus(int id)
        {
            if (!IsAuthorized(out _))
                return Json(new { success = false });

            var call = await _phoneScreenRepository.GetCallByIdAsync(id);
            if (call == null)
                return Json(new { success = false });

            return Json(new
            {
                success = true,
                callId = call.Id,
                status = CanonicalStatus(call.Status),
                rawStatus = call.Status,
                endedReason = call.EndedReason,
                result = NormalizeSuccess(call),
                summary = call.Summary,
                durationSeconds = call.DurationSeconds,
                recordingUrl = call.RecordingUrl
            });
        }

        // ────────────────────────────────────────────────────────────────────
        // Webhook — Plivo CX agent posts the call outcome here
        // ────────────────────────────────────────────────────────────────────

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook()
        {
            string rawBody = null;
            PhoneScreenHangupPayload payload = null;

            try
            {
                if (Request.HasFormContentType)
                {
                    // Plivo's RECORDING/HANGUP event callbacks may post form-encoded or
                    // multipart data (with the recording as a file part). Read the fields.
                    var form = await Request.ReadFormAsync();
                    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var kv in form)
                        if (!dict.ContainsKey(kv.Key)) dict[kv.Key] = kv.Value.ToString();
                    rawBody = JsonConvert.SerializeObject(dict);
                    payload = MapPayload(dict);
                }
                else
                {
                    using (var reader = new StreamReader(Request.Body))
                        rawBody = await reader.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(rawBody))
                        payload = ParseHangupPayload(rawBody);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Content("Error: could not read request body.", "text/html");
            }

            if (payload == null)
                return Content("Error: could not parse payload.", "text/html");

            // Resolve the call record: our row PK (log_id) → conversation_id → call_uuid.
            PhoneScreenCall existing = null;
            if (!string.IsNullOrWhiteSpace(payload.LogId) && int.TryParse(payload.LogId, out var logId))
                existing = await _phoneScreenRepository.GetCallByIdAsync(logId);

            if (existing == null && !string.IsNullOrWhiteSpace(payload.ConversationId))
                existing = await _phoneScreenRepository.GetCallByConversationIdAsync(payload.ConversationId);

            if (existing == null && !string.IsNullOrWhiteSpace(payload.CallUUID))
                existing = await _phoneScreenRepository.GetCallByExternalCallIdAsync(payload.CallUUID);

            if (existing == null)
                return Content("OK. No matching call record found.", "text/html");

            var core = ExtractCore(rawBody);
            var update = BuildUpdateFromPayload(existing.Id, payload, core, rawBody);
            await _phoneScreenRepository.UpdateCallFromWebhookAsync(update);

            return Content($"OK. callId={existing.Id}", "text/html");
        }

        // ────────────────────────────────────────────────────────────────────
        // Webhook — ElevenLabs post-call transcription for the WhatsApp screener
        // Configure at ElevenLabs → Agents → Settings → Post-call webhook:
        //   https://portal.risebird.io/PhoneScreener/ElevenLabsWebhook
        // ────────────────────────────────────────────────────────────────────

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ElevenLabsWebhook()
        {
            string rawBody;
            try
            {
                using (var reader = new StreamReader(Request.Body))
                    rawBody = await reader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(400, "Could not read request body.");
            }

            if (string.IsNullOrWhiteSpace(rawBody))
                return StatusCode(400, "Empty body.");

            var signature = Request.Headers.TryGetValue("ElevenLabs-Signature", out var sigVals)
                ? sigVals.ToString()
                : null;

            if (!_elevenLabsAgentService.VerifyWebhookSignature(rawBody, signature))
                return StatusCode(401, "Invalid signature.");

            JObject root;
            try
            {
                root = JObject.Parse(rawBody);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(400, "Invalid JSON.");
            }

            var type = (string)root["type"];
            var data = root["data"] as JObject;
            if (data == null)
                return Content("OK. No data object.", "text/html");

            var conversationId = (string)data["conversation_id"];

            // Resolve the row: log_id (our PK, echoed in dynamic_variables) → conversation_id.
            var dynamicVars = data.SelectToken("conversation_initiation_client_data.dynamic_variables") as JObject;
            var logIdStr = dynamicVars != null ? (string)dynamicVars["log_id"] : null;

            PhoneScreenCall existing = null;
            if (!string.IsNullOrWhiteSpace(logIdStr) && int.TryParse(logIdStr, out var logId))
                existing = await _phoneScreenRepository.GetCallByIdAsync(logId);
            if (existing == null && !string.IsNullOrWhiteSpace(conversationId))
                existing = await _phoneScreenRepository.GetCallByConversationIdAsync(conversationId);

            if (existing == null)
                return Content("OK. No matching screen record found.", "text/html");

            // Handle the call-initiation-failure event (candidate did not get the message / declined).
            if (string.Equals(type, "call_initiation_failure", StringComparison.OrdinalIgnoreCase))
            {
                await _phoneScreenRepository.UpdateCallFromWebhookAsync(new PhoneScreenCall
                {
                    Id = existing.Id,
                    Status = "ended",
                    EndedReason = (string)data["failure_reason"] ?? "call-initiation-failure",
                    EndedAt = DateTime.UtcNow,
                    ConversationId = conversationId,
                    RawPayload = rawBody
                });
                return Content($"OK. callId={existing.Id}", "text/html");
            }

            // Default path: post_call_transcription (full transcript + analysis + data collection).
            var update = BuildUpdateFromElevenLabs(existing.Id, data, rawBody);
            update.ConversationId = conversationId;
            await _phoneScreenRepository.UpdateCallFromWebhookAsync(update);

            return Content($"OK. callId={existing.Id}", "text/html");
        }

        /// <summary>Maps an ElevenLabs post_call_transcription <c>data</c> object onto our row.</summary>
        private static PhoneScreenCall BuildUpdateFromElevenLabs(int callId, JObject data, string rawBody)
        {
            var update = new PhoneScreenCall { Id = callId, RawPayload = rawBody };

            var status = (string)data["status"];
            update.Status = string.Equals(status, "done", StringComparison.OrdinalIgnoreCase) ? "ended" : (status ?? "ended");
            update.EndedAt = DateTime.UtcNow;

            var metadata = data["metadata"] as JObject;
            if (metadata != null)
            {
                var dur = metadata["call_duration_secs"];
                if (dur != null && int.TryParse(dur.ToString(), out var durSecs) && durSecs > 0)
                    update.DurationSeconds = durSecs;

                var cost = metadata["cost"];
                if (cost != null && decimal.TryParse(cost.ToString(), out var costVal))
                    update.Cost = costVal;

                var startUnix = metadata["start_time_unix_secs"];
                if (startUnix != null && long.TryParse(startUnix.ToString(), out var startSecs) && startSecs > 0)
                    update.StartedAt = DateTimeOffset.FromUnixTimeSeconds(startSecs).UtcDateTime;

                var termination = (string)metadata["termination_reason"];
                if (!string.IsNullOrWhiteSpace(termination))
                    update.EndedReason = termination;
            }

            update.Transcript = BuildTranscriptText(data["transcript"] as JArray);

            var analysis = data["analysis"] as JObject;
            if (analysis != null)
            {
                update.Summary = (string)analysis["transcript_summary"];
                update.SuccessEvaluation = (string)analysis["call_successful"];

                var collection = analysis["data_collection_results"] as JObject;
                var flat = FlattenDataCollection(collection);
                if (flat != null && flat.Count > 0)
                {
                    update.StructuredData = JsonConvert.SerializeObject(flat);
                    ApplyKnownDataCollectionFields(update, flat);
                }
                else if (collection != null)
                {
                    update.StructuredData = collection.ToString(Formatting.None);
                }
            }

            return update;
        }

        /// <summary>Flattens ElevenLabs data_collection_results ({ id: { value, rationale } }) to { id: value }.</summary>
        private static Dictionary<string, string> FlattenDataCollection(JObject collection)
        {
            if (collection == null) return null;
            var flat = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in collection.Properties())
            {
                string val;
                if (prop.Value is JObject obj && obj["value"] != null)
                    val = obj["value"].Type == JTokenType.Null ? null : obj["value"].ToString();
                else
                    val = prop.Value?.ToString();
                flat[prop.Name] = val;
            }
            return flat;
        }

        /// <summary>Maps recognized data-collection identifiers onto first-class result columns.</summary>
        private static void ApplyKnownDataCollectionFields(PhoneScreenCall update, Dictionary<string, string> flat)
        {
            foreach (var kv in flat)
            {
                var key = kv.Key ?? string.Empty;
                var val = kv.Value;
                if (string.IsNullOrWhiteSpace(val)) continue;

                if (update.Qualified == null && key.IndexOf("qualif", StringComparison.OrdinalIgnoreCase) >= 0)
                    update.Qualified = NormalizeYesNo(val);
                else if (update.Disposition == null && key.IndexOf("disposition", StringComparison.OrdinalIgnoreCase) >= 0)
                    update.Disposition = val;
                else if (update.FailedCriteria == null &&
                         (key.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          key.IndexOf("fail_", StringComparison.OrdinalIgnoreCase) >= 0))
                    update.FailedCriteria = val;
                else if (update.Availability == null && key.IndexOf("availab", StringComparison.OrdinalIgnoreCase) >= 0)
                    update.Availability = val;
            }
        }

        private static string NormalizeYesNo(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return val;
            var v = val.Trim().ToLowerInvariant();
            if (v == "true" || v == "yes" || v == "1") return "Yes";
            if (v == "false" || v == "no" || v == "0") return "No";
            return val;
        }

        /// <summary>Renders the ElevenLabs transcript turns as readable "Role: message" lines.</summary>
        private static string BuildTranscriptText(JArray transcript)
        {
            if (transcript == null || transcript.Count == 0) return null;
            var sb = new System.Text.StringBuilder();
            foreach (var turn in transcript)
            {
                var role = (string)turn["role"];
                var message = (string)turn["message"];
                if (string.IsNullOrWhiteSpace(message)) continue;
                var label = string.Equals(role, "agent", StringComparison.OrdinalIgnoreCase) ? "Agent" : "Candidate";
                sb.Append(label).Append(": ").AppendLine(message.Trim());
            }
            var text = sb.ToString().Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        // ────────────────────────────────────────────────────────────────────
        // Manual media backfill (button on the Call Detail screen)
        // ────────────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshMedia(int id)
        {
            if (!IsAuthorized(out _))
                return Json(new { success = false, message = "Session expired. Please sign in again." });

            try
            {
                var call = await _phoneScreenRepository.GetCallByIdAsync(id);
                if (call == null)
                    return Json(new { success = false, message = "Call not found." });

                if (string.IsNullOrWhiteSpace(call.ExternalCallId))
                    return Json(new { success = false, message = "No Plivo call UUID has been recorded for this call yet." });

                var media = await _plivoCXService.FetchCallMediaAsync(call.ExternalCallId, WebhookCallbackUrl());
                if (media == null || !media.Found)
                    return Json(new
                    {
                        success = false,
                        message = media?.Error ?? "Plivo has no recording/transcript for this call yet. It can take a short while after the call ends — try again shortly."
                    });

                await _phoneScreenRepository.UpdateCallFromWebhookAsync(new PhoneScreenCall
                {
                    Id = call.Id,
                    RecordingUrl = media.RecordingUrl,
                    Transcript = media.Transcript,
                    DurationSeconds = call.DurationSeconds ?? media.DurationSeconds
                });

                return Json(new
                {
                    success = true,
                    recordingUrl = media.RecordingUrl,
                    hasTranscript = !string.IsNullOrWhiteSpace(media.Transcript),
                    message = "Recording and transcript refreshed."
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not refresh media. Please try again." });
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// If a completed call is missing its recording or transcript, resolve them
        /// from Plivo's Voice REST APIs by call UUID and merge into the stored row.
        /// </summary>
        private async Task<PhoneScreenCall> TryBackfillMediaAsync(PhoneScreenCall call)
        {
            try
            {
                if (call == null) return call;

                var ended = CanonicalStatus(call.Status) == "ended";
                var needsMedia = string.IsNullOrWhiteSpace(call.RecordingUrl) ||
                                 string.IsNullOrWhiteSpace(call.Transcript);
                if (!ended || !needsMedia || string.IsNullOrWhiteSpace(call.ExternalCallId))
                    return call;

                var media = await _plivoCXService.FetchCallMediaAsync(call.ExternalCallId, WebhookCallbackUrl());
                if (media == null || !media.Found)
                    return call;

                // Only supply values that are currently missing (repository coalesces).
                await _phoneScreenRepository.UpdateCallFromWebhookAsync(new PhoneScreenCall
                {
                    Id = call.Id,
                    RecordingUrl = string.IsNullOrWhiteSpace(call.RecordingUrl) ? media.RecordingUrl : null,
                    Transcript = string.IsNullOrWhiteSpace(call.Transcript) ? media.Transcript : null,
                    DurationSeconds = call.DurationSeconds ?? media.DurationSeconds
                });

                return await _phoneScreenRepository.GetCallByIdAsync(call.Id) ?? call;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return call;
            }
        }

        // Property names Plivo may use for the transcript/summary that arrive as an
        // object or array rather than a plain string — capture their raw JSON whole.
        private static readonly HashSet<string> RawCaptureKeys =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "transcript", "transcription", "chat_transcript", "chat_history",
                "messages", "summary", "conversation_summary", "call_summary"
            };

        /// <summary>
        /// Parses any JSON webhook shape (flat, {"json":{...}} wrapper, or the nested
        /// Plivo CX event envelope) by flattening every leaf into a case-insensitive
        /// dictionary and mapping known field aliases. Resilient to nesting changes.
        /// </summary>
        private PhoneScreenHangupPayload ParseHangupPayload(string rawBody)
        {
            try
            {
                var root = JToken.Parse(rawBody);
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                FlattenJson(null, root, dict);
                var payload = MapPayload(dict);
                if (payload != null && dict.Count > 0)
                    return payload;
            }
            catch (Exception ex)
            {
                _ = _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        private static void FlattenJson(string name, JToken token, Dictionary<string, string> acc)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    if (name != null && RawCaptureKeys.Contains(StripNode(name)))
                        Put(acc, name, token.ToString(Formatting.None));
                    foreach (var p in ((JObject)token).Properties())
                        FlattenJson(p.Name, p.Value, acc);
                    break;
                case JTokenType.Array:
                    if (name != null && RawCaptureKeys.Contains(StripNode(name)))
                        Put(acc, name, token.ToString(Formatting.None));
                    else
                        foreach (var item in (JArray)token)
                            FlattenJson(name, item, acc);
                    break;
                default:
                    if (name != null) Put(acc, name, token.ToString());
                    break;
            }
        }

        private static void Put(Dictionary<string, string> acc, string name, string val)
        {
            if (string.IsNullOrEmpty(name)) return;
            void Set(string k)
            {
                if (!string.IsNullOrEmpty(k) &&
                    (!acc.TryGetValue(k, out var existing) || string.IsNullOrWhiteSpace(existing)))
                    acc[k] = val;
            }
            Set(name);
            Set(StripNode(name));
        }

        // Strips a node-name prefix from event_data keys, e.g. "Screener.log_id" → "log_id".
        private static string StripNode(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;
            var dot = key.LastIndexOf('.');
            return (dot >= 0 && dot < key.Length - 1) ? key.Substring(dot + 1) : key;
        }

        private static PhoneScreenHangupPayload MapPayload(Dictionary<string, string> d)
        {
            if (d == null || d.Count == 0) return null;

            string G(params string[] keys)
            {
                foreach (var k in keys)
                    if (d.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v)) return v;
                return null;
            }

            return new PhoneScreenHangupPayload
            {
                LogId          = G("log_id", "logid"),
                ConversationId = G("conversation_id", "conversationid", "conversation_uuid"),
                CallUUID       = G("call_uuid", "calluuid", "uuid", "call_id", "callid"),
                CallStatus     = G("call_status", "callstatus", "status"),
                EndedReason    = G("ended_reason", "hangup_cause", "hangup_reason", "hangup_cause_name", "hangup_source"),
                Duration       = G("duration", "recording_duration", "call_duration", "billed_duration", "duration_seconds"),
                RecordingUrl   = G("recording_url", "record_url", "recordurl", "recording_uri"),
                Transcript     = G("transcript", "transcription", "chat_transcript", "chat_history", "messages"),
                Summary        = G("observation", "analysis_observation", "disposition_reason", "summary", "conversation_summary", "call_summary", "notes"),
                Qualified      = G("qualified"),
                FailedCriteria = G("failed_criteria", "criteria_not_met"),
                Disposition    = G("disposition"),
                Degree         = G("degree"),
                GradYear       = G("grad_year"),
                Location       = G("location"),
                Joining        = G("joining"),
                Laptop         = G("laptop"),
                CompComfort    = G("comp_comfort"),
                Availability   = G("availability"),
                Notes          = G("notes"),
                CandidateName  = G("candidate_name"),
                CandidateEmail = G("candidate_email"),
                PhoneNumber    = G("phone_number")
            };
        }

        private static PhoneScreenCall BuildUpdateFromPayload(int callId, PhoneScreenHangupPayload p, JObject core, string rawBody)
        {
            int? duration = null;
            if (int.TryParse(p.Duration, out var d)) duration = d;

            // The PASS / HOLD / FAIL signal. Newer agents send an ADVANCE / HOLD /
            // REJECT disposition (with a matching analysis.verdict); older agents send
            // a yes/no "qualified" flag. Support both, verdict taking precedence.
            var verdict = core?["analysis"]?["verdict"]?.ToString();
            var successEval = MapSuccess(p.Disposition, verdict, p.Qualified);
            var qualified = MapQualified(p.Disposition, verdict, p.Qualified);

            // Capture the whole returned payload (minus routing/echo/media fields) so
            // any agent's variables — including a nested analysis object — are stored
            // and rendered by the (already generic) detail screen without code changes.
            var structuredJson = BuildStructuredData(core)
                                 ?? BuildStructuredFallback(p);

            return new PhoneScreenCall
            {
                Id = callId,
                Status = string.IsNullOrWhiteSpace(p.CallStatus) ? "completed" : p.CallStatus,
                EndedReason = p.EndedReason,
                ConversationId = p.ConversationId,
                ExternalCallId = p.CallUUID,
                EndedAt = DateTime.UtcNow,
                DurationSeconds = duration,
                RecordingUrl = p.RecordingUrl,
                Transcript = p.Transcript,
                Summary = p.Summary,
                SuccessEvaluation = successEval,
                Disposition = p.Disposition,
                Qualified = qualified,
                FailedCriteria = p.FailedCriteria,
                Availability = p.Availability,
                StructuredData = structuredJson,
                RawPayload = rawBody
            };
        }

        // Maps an agent outcome to the UI's PASS / HOLD / FAIL result.
        private static string MapSuccess(string disposition, string verdict, string qualified)
        {
            var v = (verdict ?? disposition)?.Trim().ToUpperInvariant();
            if (v == "ADVANCE") return "PASS";
            if (v == "REJECT") return "FAIL";
            if (v == "HOLD") return "HOLD";
            if (!string.IsNullOrWhiteSpace(qualified))
                return qualified.Trim().ToLowerInvariant().StartsWith("y") ? "PASS" : "FAIL";
            return null;
        }

        private static string MapQualified(string disposition, string verdict, string qualified)
        {
            var v = (verdict ?? disposition)?.Trim().ToUpperInvariant();
            if (v == "ADVANCE") return "yes";
            if (v == "REJECT") return "no";
            if (v == "HOLD") return "hold";
            if (!string.IsNullOrWhiteSpace(qualified))
                return qualified.Trim().ToLowerInvariant().StartsWith("y") ? "yes" : "no";
            return null;
        }

        // Routing/echo/media keys that should not clutter the structured display
        // (they are shown elsewhere on the screen or are transport plumbing).
        private static readonly HashSet<string> StructuredExcludeKeys =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "none", "form_data", "form_urlencoded",
                "log_id", "logid", "log_identifier",
                "call_uuid", "calluuid", "uuid", "conversation_id", "conversationid",
                "call_status", "callstatus",
                "candidate_name", "candidate_email", "phone_number",
                "recording_url", "record_url", "recordurl", "recording_uri",
                "transcript", "transcription"
            };

        /// <summary>
        /// Returns the meaningful payload object regardless of envelope: unwraps the
        /// flow HTTP node's {"json":{...}} body, folds a Plivo CX event envelope's
        /// data.object/event_data up a level, or returns a flat body as-is.
        /// </summary>
        private static JObject ExtractCore(string rawBody)
        {
            if (string.IsNullOrWhiteSpace(rawBody)) return null;
            try
            {
                if (!(JToken.Parse(rawBody) is JObject obj)) return null;

                if (obj["json"] is JObject wrapped) return wrapped;

                if (obj.SelectToken("data.object") is JObject dataObj)
                {
                    var merged = (JObject)dataObj.DeepClone();
                    if (dataObj["event_data"] is JObject ed)
                    {
                        merged.Remove("event_data");
                        foreach (var prop in ed.Properties())
                            merged[StripNode(prop.Name)] = prop.Value;
                    }
                    return merged;
                }
                return obj;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Serializes the payload as structured data, dropping routing/echo/media keys
        /// and empty scalar values. Preserves nested objects such as analysis so the
        /// detail screen can surface score, verdict, observation, and criteria.
        /// </summary>
        private static string BuildStructuredData(JObject core)
        {
            if (core == null) return null;
            var clone = (JObject)core.DeepClone();

            foreach (var prop in clone.Properties().ToList())
            {
                if (StructuredExcludeKeys.Contains(prop.Name))
                {
                    prop.Remove();
                    continue;
                }
                var val = prop.Value;
                if (val == null || val.Type == JTokenType.Null ||
                    (val.Type == JTokenType.String && string.IsNullOrWhiteSpace(val.ToString())))
                    prop.Remove();
            }

            return clone.HasValues ? clone.ToString(Formatting.None) : null;
        }

        // Absolute URL of this controller's Webhook action, used as the Plivo
        // transcription callback so a late transcript posts back and gets stored.
        private string WebhookCallbackUrl()
        {
            try { return Url.Action("Webhook", "PhoneScreener", null, Request.Scheme); }
            catch { return null; }
        }

        // Fallback structured data from the strongly-typed base-agent fields, used only
        // when the raw payload could not be parsed into an object.
        private static string BuildStructuredFallback(PhoneScreenHangupPayload p)
        {
            var structured = new Dictionary<string, object>();
            void Add(string k, string v) { if (!string.IsNullOrWhiteSpace(v)) structured[k] = v; }
            Add("qualified", p.Qualified);
            Add("failed_criteria", p.FailedCriteria);
            Add("disposition", p.Disposition);
            Add("degree", p.Degree);
            Add("grad_year", p.GradYear);
            Add("location", p.Location);
            Add("joining", p.Joining);
            Add("laptop", p.Laptop);
            Add("comp_comfort", p.CompComfort);
            Add("availability", p.Availability);
            Add("notes", p.Notes);
            return structured.Count > 0 ? JsonConvert.SerializeObject(structured) : null;
        }

        private PhoneScreenCallResultVM MapToResultVM(PhoneScreenCall call, PhoneScreenIRFHeader header)
        {
            return new PhoneScreenCallResultVM
            {
                CallId = call.Id,
                Provider = call.Provider,
                IRFId = call.IRFId,
                IRFCode = header?.IRFCode,
                IRFName = header?.IRFName,
                AgentLabel = header?.PlivoAssistantUrl,
                CandidateId = call.CandidateId,
                CandidateName = call.CandidateName,
                CandidateEmail = call.CandidateEmail,
                Phone = call.Phone,
                Status = CanonicalStatus(call.Status),
                RawStatus = call.Status,
                EndedReason = call.EndedReason,
                StartedAt = call.StartedAt,
                EndedAt = call.EndedAt,
                DurationSeconds = call.DurationSeconds,
                Cost = call.Cost,
                RecordingUrl = call.RecordingUrl,
                Transcript = call.Transcript,
                Summary = call.Summary,
                SuccessEvaluation = NormalizeSuccess(call),
                Disposition = call.Disposition,
                Qualified = call.Qualified,
                FailedCriteria = call.FailedCriteria,
                Availability = call.Availability,
                StructuredDataJson = call.StructuredData,
                RawJson = call.RawPayload
            };
        }

        private static void ComputeMetrics(PhoneScreenOutcomesVM vm, List<PhoneScreenCall> calls)
        {
            var rowsWithCall = vm.Rows.Where(r => r.HasCall).ToList();
            vm.CallsPlaced = rowsWithCall.Count;

            foreach (var r in rowsWithCall)
            {
                var isInProgress = r.Status == "queued" || r.Status == "ringing" || r.Status == "in-progress";
                var isUnanswered = IsUnanswered(r.RawStatus);
                var isPass = string.Equals(r.SuccessEvaluation, "PASS", StringComparison.OrdinalIgnoreCase);

                if (isInProgress) { vm.InProgress++; continue; }
                if (isUnanswered) { vm.Unanswered++; vm.FailedOrNoAnswer++; continue; }
                if (isPass) { vm.QualifiedPass++; }
                else { vm.ConnectedNotQualified++; }
            }
        }

        private static string CanonicalStatus(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "queued";
            switch (raw.Trim().ToLowerInvariant())
            {
                case "initiated":
                case "triggered":
                case "queued":
                    return "queued";
                case "ringing":
                    return "ringing";
                case "in-progress":
                case "in_progress":
                case "ongoing":
                    return "in-progress";
                case "completed":
                case "ended":
                case "no_answer":
                case "busy_rejected":
                case "failed":
                case "trigger-failed":
                    return "ended";
                default:
                    return "ended";
            }
        }

        private static bool IsUnanswered(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return false;
            switch (raw.Trim().ToLowerInvariant())
            {
                case "no_answer":
                case "busy_rejected":
                case "failed":
                case "trigger-failed":
                    return true;
                default:
                    return false;
            }
        }

        private static string NormalizeSuccess(PhoneScreenCall call)
        {
            if (!string.IsNullOrWhiteSpace(call.SuccessEvaluation))
            {
                var s = call.SuccessEvaluation.Trim();
                if (s.Equals("true", StringComparison.OrdinalIgnoreCase) || s.Equals("pass", StringComparison.OrdinalIgnoreCase))
                    return "PASS";
                if (s.Equals("false", StringComparison.OrdinalIgnoreCase) || s.Equals("fail", StringComparison.OrdinalIgnoreCase))
                    return "FAIL";
                return s;
            }
            if (!string.IsNullOrWhiteSpace(call.Qualified))
                return call.Qualified.Trim().ToLowerInvariant().StartsWith("y") ? "PASS" : "FAIL";
            return null;
        }

        private bool IsAuthorized(out string profileName)
        {
            profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
            var email = HttpContext.Session.GetString("email");
            return !string.IsNullOrEmpty(email)
                   && !string.IsNullOrEmpty(profileName)
                   && AllowedRoles.Contains(profileName);
        }

        private string GetSessionEmail()
            => HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
    }
}
