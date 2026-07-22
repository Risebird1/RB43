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
            IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _phoneScreenRepository = phoneScreenRepository;
            _irfService = irfService;
            _plivoCXService = plivoCXService;
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
                candidate.Phone = "+918105232468";

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
            string rawBody;
            try
            {
                using (var reader = new StreamReader(Request.Body))
                    rawBody = await reader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Content("Error: could not read request body.", "text/html");
            }

            if (string.IsNullOrWhiteSpace(rawBody))
                return Content("Error: empty request body.", "text/html");

            var payload = ParseHangupPayload(rawBody);
            if (payload == null)
                return Content("Error: could not parse payload.", "text/html");

            // Resolve the call record: log_id (our row PK) first, then conversation_id.
            PhoneScreenCall existing = null;
            if (!string.IsNullOrWhiteSpace(payload.LogId) && int.TryParse(payload.LogId, out var logId))
                existing = await _phoneScreenRepository.GetCallByIdAsync(logId);

            if (existing == null && !string.IsNullOrWhiteSpace(payload.ConversationId))
                existing = await _phoneScreenRepository.GetCallByConversationIdAsync(payload.ConversationId);

            if (existing == null)
                return Content("OK. No matching call record found.", "text/html");

            var update = BuildUpdateFromPayload(existing.Id, payload, rawBody);
            await _phoneScreenRepository.UpdateCallFromWebhookAsync(update);

            return Content($"OK. callId={existing.Id}", "text/html");
        }

        // ────────────────────────────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────────────────────────────

        private PhoneScreenHangupPayload ParseHangupPayload(string rawBody)
        {
            // Strategy 1: flat JSON
            try
            {
                var flat = JsonConvert.DeserializeObject<PhoneScreenHangupPayload>(rawBody,
                    new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });
                if (flat != null && (!string.IsNullOrEmpty(flat.LogId) || !string.IsNullOrEmpty(flat.ConversationId)))
                    return flat;
            }
            catch (Exception ex)
            {
                _ = _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            // Strategy 2: wrapper {"none":{},"json":{...}} used by flow HTTP nodes
            try
            {
                var wrapper = JsonConvert.DeserializeObject<Dictionary<string, object>>(rawBody);
                if (wrapper != null && wrapper.ContainsKey("json"))
                {
                    var innerJson = wrapper["json"]?.ToString();
                    if (!string.IsNullOrEmpty(innerJson))
                        return JsonConvert.DeserializeObject<PhoneScreenHangupPayload>(innerJson,
                            new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });
                }
            }
            catch (Exception ex)
            {
                _ = _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            // Strategy 3: nested Plivo CX event envelope — flatten event_data.*
            try
            {
                var root = JObject.Parse(rawBody);
                var ed = root.SelectToken("data.object.event_data") ?? root.SelectToken("event_data");
                if (ed != null)
                {
                    string Val(string key)
                    {
                        var direct = ed[key];
                        if (direct != null) return direct.ToString();
                        // keys may be prefixed with node name e.g. "Screener.log_id"
                        foreach (var prop in ed.Children<JProperty>())
                            if (prop.Name.EndsWith("." + key, StringComparison.OrdinalIgnoreCase) ||
                                prop.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                                return prop.Value?.ToString();
                        return null;
                    }

                    return new PhoneScreenHangupPayload
                    {
                        ConversationId = root.SelectToken("data.object.conversation_id")?.ToString(),
                        CallUUID = root.SelectToken("data.object.call_uuid")?.ToString(),
                        LogId = Val("log_id"),
                        CallStatus = Val("call_status"),
                        EndedReason = Val("ended_reason"),
                        Duration = Val("duration"),
                        RecordingUrl = Val("recording_url"),
                        Transcript = Val("transcript"),
                        Summary = Val("summary"),
                        Qualified = Val("qualified"),
                        FailedCriteria = Val("failed_criteria"),
                        Disposition = Val("disposition"),
                        Degree = Val("degree"),
                        GradYear = Val("grad_year"),
                        Location = Val("location"),
                        Joining = Val("joining"),
                        Laptop = Val("laptop"),
                        CompComfort = Val("comp_comfort"),
                        Availability = Val("availability"),
                        Notes = Val("notes")
                    };
                }
            }
            catch (Exception ex)
            {
                _ = _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return null;
        }

        private static PhoneScreenCall BuildUpdateFromPayload(int callId, PhoneScreenHangupPayload p, string rawBody)
        {
            int? duration = null;
            if (int.TryParse(p.Duration, out var d)) duration = d;

            string qualified = null;
            if (!string.IsNullOrWhiteSpace(p.Qualified))
                qualified = p.Qualified.Trim().ToLowerInvariant().StartsWith("y") ? "yes" : "no";

            string successEval = null;
            if (qualified == "yes") successEval = "PASS";
            else if (qualified == "no") successEval = "FAIL";

            // Assemble the arbitrary structured output the detail screen renders dynamically.
            var structured = new Dictionary<string, object>();
            void Add(string k, string v) { if (!string.IsNullOrWhiteSpace(v)) structured[k] = v; }
            Add("qualified", qualified);
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
                StructuredData = structured.Count > 0 ? JsonConvert.SerializeObject(structured) : null,
                RawPayload = rawBody
            };
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
