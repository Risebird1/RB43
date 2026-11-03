using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.Business.PlivoCX;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using Risebird.Web.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// PlivoCX campaigns: operator-run AI scheduling / rescheduling / attendance-confirmation
    /// calling, modelled on the AI Phone Screen campaigns. Create a campaign from an IRF or one of
    /// the centralized lists, set its calling policy, then Call now or Start automatic (the
    /// PlivoCXCampaignScheduler timer) and follow every call. Dialling, the state machine and the
    /// scheduler live in <see cref="IPlivoCXCampaignService"/>; the call itself, its webhook,
    /// booking, transcript and recording are the existing PlivoCX machinery.
    /// Allowed: Admin, SupportAdmin, SeniorHR, HR.
    /// </summary>
    [Authorize]
    [Route("PlivoCXCampaign")]
    public class PlivoCXCampaignController : Controller
    {
        private static readonly string[] AllowedProfiles = { "Admin", "SupportAdmin", "SeniorHR", "HR" };

        private readonly IConfiguration             _configuration;
        private readonly IPlivoCXCampaignService    _campaignService;
        private readonly IPlivoCXCampaignRepository _campaignRepository;
        private readonly IPlivoCXCallRepository     _callRepository;
        private readonly ICampaignCandidateSourceService _sources;
        private readonly IApplicationErrorLogger    _logger;

        // Cache-key prefix of this feature's copies of the centralized lists.
        private const string ListCachePrefix = "PlivoCXCampaign";

        public PlivoCXCampaignController(
            IConfiguration             configuration,
            IPlivoCXCampaignService    campaignService,
            IPlivoCXCampaignRepository campaignRepository,
            IPlivoCXCallRepository     callRepository,
            ICampaignCandidateSourceService sources,
            IApplicationErrorLogger    logger)
        {
            _configuration      = configuration;
            _campaignService    = campaignService;
            _campaignRepository = campaignRepository;
            _callRepository     = callRepository;
            _sources            = sources;
            _logger             = logger;
        }

        // ── Access ────────────────────────────────────────────────────────────────

        /// <summary>The signed-in user's email when their profile may run campaigns; otherwise null.</summary>
        private string CurrentUser()
        {
            var email = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
            var profile = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
            if (string.IsNullOrWhiteSpace(email) || Array.IndexOf(AllowedProfiles, profile) < 0) return null;
            return email;
        }

        private IActionResult Denied(bool json) => json
            ? Json(new { ok = false, message = "You do not have access to PlivoCX campaigns." })
            : RedirectToAction("SignOut", "Session");

        // ── Screen 1: Campaigns list ──────────────────────────────────────────────

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(string search = null)
        {
            if (CurrentUser() == null) return Denied(json: false);
            ViewBag.Search = search;
            return View(await _campaignRepository.ListCampaignsAsync(search));
        }

        // ── Screen 2: Create campaign ─────────────────────────────────────────────

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            if (CurrentUser() == null) return Denied(json: false);
            ViewBag.Agents = (await _campaignService.GetAgentsAsync())
                .Where(a => string.Equals(a.TargetType, "Candidate", StringComparison.OrdinalIgnoreCase)).ToList();
            ViewBag.IRFs = await _sources.GetActiveIRFsAsync();
            // The Not-scheduled and Reschedule lists scope HR users to their own skill channels,
            // exactly as /Client/NotScheduledMeetings and /Client/ReScheduleMeetings do.
            ViewBag.ChannelScoped = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "") == "HR";
            return View();
        }

        [HttpGet("Agents")]
        public async Task<IActionResult> Agents()
        {
            if (CurrentUser() == null) return Denied(json: true);
            var agents = await _campaignService.GetAgentsAsync();
            return Json(agents.Select(a => new { a.Id, a.Name, a.Description, a.Purpose, a.TargetType, a.CollectsSlot, a.Channel,
                Provider = PlivoCXCampaignProvider.Normalize(a.Provider) }));
        }

        /// <summary>By IRF: every candidate of the IRF, labelled by the agent's eligibility procedure.</summary>
        [HttpGet("CandidatesForIRF")]
        public async Task<IActionResult> CandidatesForIRF(int irfId, int agentId)
        {
            if (CurrentUser() == null) return Denied(json: true);
            try
            {
                var agent = await _campaignRepository.GetAgentAsync(agentId);
                if (agent == null) return Json(new { ok = false, message = "Pick an agent first." });
                string purpose = (agent.Purpose ?? "schedule").ToLowerInvariant();

                var rows = await _sources.GetIRFRowsAsync(irfId);
                var eligible = purpose == "schedule"
                    ? await _callRepository.GetEligibleCandidatesForCallsAsync(irfId: irfId, bypassCaps: true)
                    : purpose == "reschedule"
                        ? await _callRepository.GetEligibleCandidatesForRescheduleAsync(irfId: irfId, bypassCaps: true)
                        : new List<VapiEligibleCandidateVM>();
                var eligibleById = eligible.GroupBy(e => e.CandidateId).ToDictionary(g => g.Key, g => g.First());

                var items = rows.Select(r =>
                {
                    eligibleById.TryGetValue(r.CandidateId, out var e);
                    var item = new PlivoCXCampaignPickerItem
                    {
                        CandidateId  = r.CandidateId,
                        Name         = r.CandidateName,
                        Email        = r.CandidateEmail,
                        Phone        = r.CandidatePhone,
                        IrfId        = r.IRFId,
                        IrfName      = r.IRFName,
                        JobTitle     = r.JobTitle,
                        ClientName   = r.ClientName,
                        InterviewId  = purpose == "confirm" ? r.UpcomingInterviewId : null,
                        OriginalDate = purpose == "confirm" ? r.UpcomingInterviewDate
                                     : purpose == "reschedule" ? (e?.OriginalInterviewDate ?? r.LastInterviewDate)
                                     : null
                    };
                    if (string.IsNullOrWhiteSpace(r.CandidatePhone)) item.Eligibility = "no-number";
                    else if (r.IsCold) { item.Eligibility = "cold"; item.Note = "marked cold"; }
                    else if (string.Equals(r.PlivoCXDisposition, "uninterested", StringComparison.OrdinalIgnoreCase)) { item.Eligibility = "not-eligible"; item.Note = "not interested"; }
                    else if (purpose == "confirm") { item.Eligibility = r.HasMeeting ? "eligible" : "not-eligible"; if (!r.HasMeeting) item.Note = "no upcoming meeting"; }
                    else if (e != null) item.Eligibility = "eligible";
                    else if (r.HasMeeting) item.Eligibility = "has-meeting";
                    else { item.Eligibility = "not-eligible"; item.Note = purpose == "reschedule" ? "no meeting to reschedule" : "already booked or recently scheduled"; }

                    if (!string.IsNullOrWhiteSpace(r.ActiveCampaignName))
                        item.Note = (item.Note == null ? "" : item.Note + " · ") + "in campaign \"" + r.ActiveCampaignName + "\"";
                    item.Selected = item.Eligibility == "eligible" || item.Eligibility == "has-meeting";
                    return item;
                }).ToList();

                return Json(new { ok = true, purpose, candidates = items });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Could not load the IRF's candidates." });
            }
        }

        /// <summary>Not-scheduled list (/Client/NotScheduledMeetings), across clients for internal users.</summary>
        [HttpGet("CandidatesFromNotScheduled")]
        public async Task<IActionResult> CandidatesFromNotScheduled(bool refresh = false)
        {
            var email = CurrentUser();
            if (email == null) return Denied(json: true);
            try
            {
                var (items, loadedAt) = await _sources.NotScheduledAsync(ListCachePrefix, email, refresh);
                return Json(new { ok = true, purpose = "schedule", candidates = items, loadedAt = CampaignCandidateSourceService.LoadedAtText(loadedAt) });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Could not load the not-scheduled list." });
            }
        }

        /// <summary>Reschedule list (/Client/ReScheduleMeetings) for a meeting-date range (default: the last month).</summary>
        [HttpGet("CandidatesFromReschedule")]
        public async Task<IActionResult> CandidatesFromReschedule(DateTime? startDate = null, DateTime? endDate = null, bool refresh = false)
        {
            var email = CurrentUser();
            if (email == null) return Denied(json: true);
            try
            {
                var (items, loadedAt) = await _sources.RescheduleAsync(ListCachePrefix, email, startDate, endDate, refresh);
                return Json(new { ok = true, purpose = "reschedule", candidates = items, loadedAt = CampaignCandidateSourceService.LoadedAtText(loadedAt) });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Could not load the reschedule list." });
            }
        }

        /// <summary>Unconfirmed list (/Dashboard/UnconfirmedCandidates): Ops-wide, meetings in the next 7 days.</summary>
        [HttpGet("CandidatesFromUnconfirmed")]
        public async Task<IActionResult> CandidatesFromUnconfirmed(bool refresh = false)
        {
            if (CurrentUser() == null) return Denied(json: true);
            try
            {
                var (items, loadedAt) = await _sources.UnconfirmedAsync(ListCachePrefix, refresh);
                return Json(new { ok = true, purpose = "confirm", candidates = items, loadedAt = CampaignCandidateSourceService.LoadedAtText(loadedAt) });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Could not load the unconfirmed list." });
            }
        }

        /// <summary>
        /// Creates the campaign (Draft) and one candidate row per ticked candidate. The form posts
        /// the selection as JSON in <paramref name="payload"/> so the antiforgery token travels as
        /// a normal form field.
        /// </summary>
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string payload)
        {
            var email = CurrentUser();
            if (email == null) return Denied(json: true);
            try
            {
                var req = string.IsNullOrWhiteSpace(payload) ? null : JsonConvert.DeserializeObject<PlivoCXCampaignCreateRequest>(payload);
                if (req == null) return Json(new { ok = false, message = "Nothing was posted." });
                req.Name = req.Name?.Trim();
                if (string.IsNullOrWhiteSpace(req.Name)) return Json(new { ok = false, message = "Give the campaign a name." });
                if (req.Name.Length > 200) req.Name = req.Name.Substring(0, 200);

                var provider = PlivoCXCampaignProvider.Normalize(req.Provider);
                if (provider == null || !PlivoCXCampaignProvider.IsEnabled(provider))
                    return Json(new { ok = false, message = (provider ?? req.Provider) + " campaigns are not available yet. Pick Plivo or ElevenLabs." });

                var agent = await _campaignRepository.GetAgentAsync(req.AgentId);
                if (agent == null || !agent.IsActive) return Json(new { ok = false, message = "Pick an agent." });
                if (!string.Equals(agent.TargetType, "Candidate", StringComparison.OrdinalIgnoreCase))
                    return Json(new { ok = false, message = "This agent calls interviewers; campaigns call candidates." });
                if (PlivoCXCampaignProvider.Normalize(agent.Provider) != provider)
                    return Json(new { ok = false, message = $"That agent runs on {PlivoCXCampaignProvider.Normalize(agent.Provider)}, not {provider}. Pick a {provider} agent." });

                var picked = (req.Candidates ?? new List<PlivoCXCampaignPickerItem>())
                    .Where(c => c.CandidateId > 0)
                    .GroupBy(c => c.CandidateId).Select(g => g.First()).ToList();
                if (picked.Count == 0) return Json(new { ok = false, message = "Tick at least one candidate." });

                var source = req.SourceType ?? PlivoCXCampaignSource.IRF;
                var campaign = new PlivoCXCampaign
                {
                    Name        = req.Name,
                    SourceType  = source,
                    Provider    = provider,
                    AgentId     = agent.Id,
                    CallPurpose = string.IsNullOrWhiteSpace(agent.Purpose) ? "schedule" : agent.Purpose,
                    Status      = PlivoCXCampaignStatus.Draft,
                    CreatedBy   = email
                };
                if (source == PlivoCXCampaignSource.IRF && req.IRFId.HasValue)
                {
                    campaign.IRFId = req.IRFId;
                    campaign.IRFName = picked.First().IrfName;
                    campaign.ClientName = picked.First().ClientName;
                }
                else
                {
                    campaign.IRFName = PlivoCXCampaignSource.ListName(source) ?? source;
                    var clients = picked.Select(p => p.ClientName).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
                    var irfs = picked.Select(p => p.IrfName).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
                    campaign.ClientName = (irfs.Count == 1 ? irfs[0] : (irfs.Count + " IRFs"))
                                        + " · " + (clients.Count == 1 ? clients[0] : (clients.Count + " clients"));
                }

                int id = await _campaignRepository.InsertCampaignAsync(campaign);
                if (id <= 0) return Json(new { ok = false, message = "The campaign could not be saved. Have the PlivoCX campaign tables been deployed?" });

                var rows = picked.Select(p => new PlivoCXCampaignCandidate
                {
                    CandidateId           = p.CandidateId,
                    CandidateName         = Cut(p.Name, 200),
                    CandidateEmail        = Cut(p.Email, 200),
                    CandidatePhone        = Cut(p.Phone?.Trim(), 30),
                    IRFId                 = p.IrfId,
                    IRFName               = Cut(p.IrfName, 100),
                    JobTitle              = Cut(p.JobTitle, 255),
                    ClientName            = Cut(p.ClientName, 255),
                    InterviewId           = p.InterviewId,
                    OriginalInterviewDate = Cut(p.OriginalDate, 50),
                    // Candidates without a valid number join as Cold, as in the reference.
                    State                 = string.IsNullOrWhiteSpace(p.Phone) ? PlivoCXCampaignCandidateState.Cold : PlivoCXCampaignCandidateState.Pending,
                    LastDisposition       = string.IsNullOrWhiteSpace(p.Phone) ? "no-number" : null
                }).ToList();
                int added = await _campaignRepository.AddCampaignCandidatesAsync(id, rows);

                return Json(new { ok = true, id, added, redirect = Url.Action(nameof(Detail), new { id }) });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "The campaign could not be created: " + ex.Message });
            }
        }

        // ── Screen 3: Campaign detail + policy ────────────────────────────────────

        [HttpGet("{id:int}")]
        [HttpGet("Detail/{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {
            if (CurrentUser() == null) return Denied(json: false);
            var campaign = await _campaignRepository.GetCampaignAsync(id);
            if (campaign == null) return NotFound();

            var vm = new PlivoCXCampaignDetailVM
            {
                Campaign           = campaign,
                Agent              = await _campaignRepository.GetAgentAsync(campaign.AgentId),
                Candidates         = await _campaignRepository.GetCampaignCandidatesAsync(id),
                CallStats          = await _campaignRepository.GetCampaignCallStatsAsync(id, _campaignService.InFlightTimeoutMinutes),
                WhatsAppConfigured = !string.IsNullOrWhiteSpace(_configuration["PlivoCX:WhatsAppAgentId"]),
                TimeZone           = PlivoCXCampaignRules.ResolveTimeZone(campaign.TimeZone),
                ConcurrencyBudget  = _campaignService.ConcurrencyBudgetFor(campaign.Provider)
            };
            return View(vm);
        }

        /// <summary>Live tiles and table for the detail page after an action.</summary>
        [HttpGet("{id:int}/Counts")]
        public async Task<IActionResult> Counts(int id)
        {
            if (CurrentUser() == null) return Denied(json: true);
            var list = await _campaignRepository.GetCampaignCandidatesAsync(id);
            var stats = await _campaignRepository.GetCampaignCallStatsAsync(id, _campaignService.InFlightTimeoutMinutes);
            return Json(new
            {
                ok = true,
                booked    = list.Count(c => c.State == PlivoCXCampaignCandidateState.Booked),
                notBooked = list.Count(c => c.State == PlivoCXCampaignCandidateState.NotBooked),
                cold      = list.Count(c => c.State == PlivoCXCampaignCandidateState.Cold),
                pending   = list.Count(c => c.State == PlivoCXCampaignCandidateState.Pending),
                total     = list.Count,
                pickup    = stats.PickupPercent,
                inFlight  = stats.InFlightCalls
            });
        }

        [HttpPost("{id:int}/SavePolicy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePolicy(int id, string windowOpens, string windowCloses, string timeZone,
            int attemptsPerDay, int totalAttempts, int minGapMinutes, int concurrentCalls, DateTime? startsOn, DateTime? endsOn)
        {
            if (CurrentUser() == null) return Denied(json: true);
            var campaign = await _campaignRepository.GetCampaignAsync(id);
            if (campaign == null) return Json(new { ok = false, message = "Campaign not found." });

            if (!TimeSpan.TryParseExact(windowOpens ?? "", @"hh\:mm", CultureInfo.InvariantCulture, out var opens)
                || !TimeSpan.TryParseExact(windowCloses ?? "", @"hh\:mm", CultureInfo.InvariantCulture, out var closes))
                return Json(new { ok = false, message = "Window times must be HH:mm." });

            campaign.WindowOpens     = opens;
            campaign.WindowCloses    = closes;
            campaign.TimeZone        = string.IsNullOrWhiteSpace(timeZone) ? PlivoCXCampaignRules.IndiaTimeZoneId : timeZone.Trim();
            campaign.AttemptsPerDay  = attemptsPerDay;
            campaign.TotalAttempts   = totalAttempts;
            campaign.MinGapMinutes   = minGapMinutes;
            campaign.ConcurrentCalls = concurrentCalls;
            campaign.StartsOn        = startsOn?.Date;
            campaign.EndsOn          = endsOn?.Date;

            var error = PlivoCXCampaignRules.ValidatePolicy(campaign);
            if (error != null) return Json(new { ok = false, message = error });

            try
            {
                await _campaignRepository.UpdateCampaignPolicyAsync(campaign);
                return Json(new { ok = true, message = "Policy saved. It applies from the next dial." });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "The policy could not be saved." });
            }
        }

        /// <summary>Dials the due candidates now, up to Concurrent - ignoring the window, keeping the attempt caps.</summary>
        [HttpPost("{id:int}/CallNow")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CallNow(int id)
        {
            var email = CurrentUser();
            if (email == null) return Denied(json: true);
            var campaign = await _campaignRepository.GetCampaignAsync(id);
            if (campaign == null) return Json(new { ok = false, message = "Campaign not found." });
            if (campaign.Status == PlivoCXCampaignStatus.Paused)
                return Json(new { ok = false, message = "The campaign is paused. Start automatic calling or un-pause it first." });

            var r = await _campaignService.DialDueCandidatesAsync(campaign, email, respectWindow: false);
            return Json(new { ok = true, dialled = r.Dialled, failed = r.Failed, remaining = r.Remaining, inFlight = r.InFlight, message = r.Message });
        }

        [HttpPost("{id:int}/StartAutomatic")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> StartAutomatic(int id) => SetStatus(id, PlivoCXCampaignStatus.Running);

        [HttpPost("{id:int}/Pause")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Pause(int id) => SetStatus(id, PlivoCXCampaignStatus.Paused);

        private async Task<IActionResult> SetStatus(int id, string status)
        {
            if (CurrentUser() == null) return Denied(json: true);
            var campaign = await _campaignRepository.GetCampaignAsync(id);
            if (campaign == null) return Json(new { ok = false, message = "Campaign not found." });
            if (status == PlivoCXCampaignStatus.Running && await _campaignRepository.CountPendingAsync(id) == 0)
                return Json(new { ok = false, message = "Nobody is pending - there is nothing to call." });

            await _campaignRepository.SetCampaignStatusAsync(id, status);
            return Json(new
            {
                ok = true,
                status,
                message = status == PlivoCXCampaignStatus.Running
                    ? "Automatic calling started. The scheduler dials inside the calling window every 2 minutes."
                    : "Paused. No new calls; calls already connecting will finish."
            });
        }

        /// <summary>Dials one typed number through the campaign's agent, to hear it before launching.</summary>
        [HttpPost("{id:int}/TestCall")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestCall(int id, string phone, string name, string email)
        {
            var user = CurrentUser();
            if (user == null) return Denied(json: true);
            var campaign = await _campaignRepository.GetCampaignAsync(id);
            if (campaign == null) return Json(new { ok = false, message = "Campaign not found." });

            var r = await _campaignService.TestCallAsync(campaign, phone, name, email, user);
            return Json(new
            {
                ok = r.Success,
                logId = r.CallLogId,
                message = r.Success ? "Test call placed. It shows in Call detail once it ends." : r.Error
            });
        }

        /// <summary>Dials one candidate row on demand (puts a resolved row back to Pending first).</summary>
        [HttpPost("CallCandidate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CallCandidate(int campaignId, int candidateId)
        {
            var user = CurrentUser();
            if (user == null) return Denied(json: true);
            var campaign = await _campaignRepository.GetCampaignAsync(campaignId);
            var cc = await _campaignRepository.GetCampaignCandidateAsync(campaignId, candidateId);
            if (campaign == null || cc == null) return Json(new { ok = false, message = "Candidate not found in this campaign." });

            if (cc.LastCallLogId.HasValue)
            {
                var last = await _callRepository.GetCallLogByIdAsync(cc.LastCallLogId.Value);
                if (last != null && (last.CallStatus == "initiated" || last.CallStatus == "triggered")
                    && last.CreatedAt >= DateTime.UtcNow.AddMinutes(-_campaignService.InFlightTimeoutMinutes))
                    return Json(new { ok = false, message = "A call to this candidate is still connecting." });
            }
            if (cc.State != PlivoCXCampaignCandidateState.Pending)
            {
                await _campaignRepository.SetCampaignCandidateStateAsync(cc.Id, PlivoCXCampaignCandidateState.Pending, null, null, null);
                cc.State = PlivoCXCampaignCandidateState.Pending;
            }

            var r = await _campaignService.DialCandidateAsync(campaign, cc, user);
            return Json(new { ok = r.Success, logId = r.CallLogId, message = r.Success ? "Calling " + cc.CandidateName + "…" : r.Error });
        }

        /// <summary>Manual state override from the Mark… menu.</summary>
        [HttpPost("MarkCandidate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkCandidate(int campaignId, int candidateId, string state)
        {
            if (CurrentUser() == null) return Denied(json: true);
            if (!PlivoCXCampaignCandidateState.IsValid(state)) return Json(new { ok = false, message = "Unknown state." });
            var cc = await _campaignRepository.GetCampaignCandidateAsync(campaignId, candidateId);
            if (cc == null) return Json(new { ok = false, message = "Candidate not found in this campaign." });

            await _campaignRepository.SetCampaignCandidateStateAsync(cc.Id, state, "marked", null, null);
            return Json(new { ok = true, message = (cc.CandidateName ?? "Candidate") + " marked " + StateLabel(state) + "." });
        }

        // ── Screen 4: Call detail ─────────────────────────────────────────────────

        [HttpGet("CallDetail/{logId:int}")]
        public async Task<IActionResult> CallDetail(int logId)
        {
            if (CurrentUser() == null) return Denied(json: false);
            var log = await _callRepository.GetCallLogByIdAsync(logId);
            if (log == null) return NotFound();

            var vm = new PlivoCXCampaignCallDetailVM { CallLog = log };
            if (log.CampaignId.HasValue)
            {
                vm.Campaign = await _campaignRepository.GetCampaignAsync(log.CampaignId.Value);
                if (vm.Campaign != null)
                {
                    vm.Agent = await _campaignRepository.GetAgentAsync(vm.Campaign.AgentId);
                    if (log.CandidateId.HasValue)
                        vm.CandidateState = (await _campaignRepository.GetCampaignCandidateAsync(vm.Campaign.Id, log.CandidateId.Value))?.State;
                }
            }
            return View(vm);
        }

        public static string StateLabel(string state) => state == PlivoCXCampaignCandidateState.NotBooked ? "Not booked" : state;

        private static string Cut(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max);
    }
}
