using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.Business.PlivoCX;
using Risebird.Business.WhatsAppCampaigns;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.Data.WhatsAppCampaignModels;
using Risebird.Entities.Enum;
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
    /// WhatsApp campaigns (hand-off: WhatsApp Campaign pages): ops send C1 (schedule invite), C2 (reschedule
    /// invite) or C3 (confirmation reminder) to a chosen list of candidates, re-send on a schedule, and track
    /// who booked or confirmed. A copy of the PlivoCX campaigns with the calls replaced by WhatsApp sends; the
    /// candidate lists are shared with them (ICampaignCandidateSourceService), the sending lives in
    /// <see cref="IWhatsAppCampaignService"/>, and the WhatsAppCampaignScheduler timer runs Running campaigns.
    /// Allowed: Admin, SupportAdmin, SeniorHR, HR.
    /// </summary>
    [Authorize]
    [Route("WhatsAppCampaign")]
    public class WhatsAppCampaignController : Controller
    {
        private static readonly string[] AllowedProfiles = { "Admin", "SupportAdmin", "SeniorHR", "HR" };

        // Cache-key prefix of this feature's copies of the centralized lists.
        private const string ListCachePrefix = "WhatsAppCampaign";

        private readonly IWhatsAppCampaignService        _campaignService;
        private readonly IWhatsAppCampaignRepository     _campaignRepository;
        private readonly ICampaignCandidateSourceService _sources;
        private readonly IApplicationErrorLogger         _logger;

        public WhatsAppCampaignController(
            IWhatsAppCampaignService        campaignService,
            IWhatsAppCampaignRepository     campaignRepository,
            ICampaignCandidateSourceService sources,
            IApplicationErrorLogger         logger)
        {
            _campaignService    = campaignService;
            _campaignRepository = campaignRepository;
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
            ? Json(new { ok = false, message = "You do not have access to WhatsApp campaigns." })
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

        /// <param name="source">The tab to open on: IRF | NotScheduled | Reschedule | Unconfirmed (from the list pages' buttons).</param>
        [HttpGet("Create")]
        public async Task<IActionResult> Create(string source = null)
        {
            if (CurrentUser() == null) return Denied(json: false);
            ViewBag.IRFs = await _sources.GetActiveIRFsAsync();
            ViewBag.Source = source == PlivoCXCampaignSource.NotScheduled || source == PlivoCXCampaignSource.Reschedule || source == PlivoCXCampaignSource.Unconfirmed
                ? source : PlivoCXCampaignSource.IRF;
            // The Not-scheduled and Reschedule lists scope HR users to their own skill channels,
            // exactly as /Client/NotScheduledMeetings and /Client/ReScheduleMeetings do.
            ViewBag.ChannelScoped = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "") == "HR";
            ViewBag.Limits = new Dictionary<string, int>
            {
                [WhatsAppCampaignPurpose.Schedule]   = _campaignService.SendLimitFor(WhatsAppCampaignPurpose.Schedule),
                [WhatsAppCampaignPurpose.Reschedule] = _campaignService.SendLimitFor(WhatsAppCampaignPurpose.Reschedule)
            };
            return View();
        }

        /// <summary>By IRF (C1): every candidate of the IRF with their WhatsApp eligibility and "n/3 used".</summary>
        [HttpGet("CandidatesForIRF")]
        public async Task<IActionResult> CandidatesForIRF(int irfId)
        {
            if (CurrentUser() == null) return Denied(json: true);
            try
            {
                var rows = await _sources.GetIRFRowsAsync(irfId);
                var items = rows.Select(r =>
                {
                    var item = new WhatsAppCampaignPickerItem
                    {
                        CandidateId = r.CandidateId,
                        Name        = r.CandidateName,
                        Email       = r.CandidateEmail,
                        Phone       = r.CandidatePhone,
                        IrfId       = r.IRFId,
                        IrfName     = r.IRFName,
                        JobTitle    = r.JobTitle,
                        ClientName  = r.ClientName,
                        Eligibility = "eligible"
                    };
                    // An invite to schedule goes to candidates without a meeting; one already booked is shown, unticked.
                    if (r.HasMeeting) { item.Eligibility = "has-meeting"; item.Note = "already has a meeting " + r.UpcomingInterviewDate; }
                    else if (r.IsCold) { item.Eligibility = "has-meeting"; item.Note = "marked cold"; }
                    return item;
                });
                var annotated = await _campaignService.AnnotateAsync(items, WhatsAppCampaignPurpose.Schedule);
                return Json(new { ok = true, purpose = WhatsAppCampaignPurpose.Schedule, candidates = annotated });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Could not load the IRF's candidates." });
            }
        }

        /// <summary>Not-scheduled list (/Client/NotScheduledMeetings) → C1.</summary>
        [HttpGet("CandidatesFromNotScheduled")]
        public async Task<IActionResult> CandidatesFromNotScheduled(bool refresh = false)
        {
            var email = CurrentUser();
            if (email == null) return Denied(json: true);
            try
            {
                var (items, loadedAt) = await _sources.NotScheduledAsync(ListCachePrefix, email, refresh);
                return await PickerJson(items, WhatsAppCampaignPurpose.Schedule, loadedAt);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Could not load the not-scheduled list." });
            }
        }

        /// <summary>Reschedule list (/Client/ReScheduleMeetings) for a meeting-date range (default: the last month) → C2.</summary>
        [HttpGet("CandidatesFromReschedule")]
        public async Task<IActionResult> CandidatesFromReschedule(DateTime? startDate = null, DateTime? endDate = null, bool refresh = false)
        {
            var email = CurrentUser();
            if (email == null) return Denied(json: true);
            try
            {
                var (items, loadedAt) = await _sources.RescheduleAsync(ListCachePrefix, email, startDate, endDate, refresh);
                return await PickerJson(items, WhatsAppCampaignPurpose.Reschedule, loadedAt);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Could not load the reschedule list." });
            }
        }

        /// <summary>Unconfirmed list (/Dashboard/UnconfirmedCandidates): meetings in the next 7 days → C3.</summary>
        [HttpGet("CandidatesFromUnconfirmed")]
        public async Task<IActionResult> CandidatesFromUnconfirmed(bool refresh = false)
        {
            if (CurrentUser() == null) return Denied(json: true);
            try
            {
                var (items, loadedAt) = await _sources.UnconfirmedAsync(ListCachePrefix, refresh);
                return await PickerJson(items, WhatsAppCampaignPurpose.Confirm, loadedAt);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Could not load the unconfirmed list." });
            }
        }

        /// <summary>The shared list rows (copied, never the cached objects) with the WhatsApp facts added.</summary>
        private async Task<IActionResult> PickerJson(List<PlivoCXCampaignPickerItem> items, string purpose, DateTime loadedAt)
        {
            var copies = items.Select(i => new WhatsAppCampaignPickerItem
            {
                CandidateId  = i.CandidateId,
                Name         = i.Name,
                Email        = i.Email,
                Phone        = i.Phone,
                IrfId        = i.IrfId,
                IrfName      = i.IrfName,
                JobTitle     = i.JobTitle,
                ClientName   = i.ClientName,
                InterviewId  = i.InterviewId,
                OriginalDate = i.OriginalDate,
                // The lists judge the calling number; WhatsApp eligibility is worked out again from the WhatsApp number.
                Eligibility  = i.Eligibility == "not-eligible" ? "not-eligible" : "eligible",
                Note         = i.Eligibility == "not-eligible" ? i.Note : null
            }).ToList();
            if (purpose == WhatsAppCampaignPurpose.Confirm)
                foreach (var c in copies.Where(c => !c.InterviewId.HasValue && c.Eligibility != "not-eligible"))
                {
                    c.Eligibility = "not-eligible";
                    c.Note = "no meeting to confirm";
                }
            var annotated = await _campaignService.AnnotateAsync(copies, purpose);
            return Json(new { ok = true, purpose, candidates = annotated, loadedAt = CampaignCandidateSourceService.LoadedAtText(loadedAt) });
        }

        /// <summary>
        /// Creates the campaign (Draft) and one candidate row per ticked candidate. The source sets the purpose
        /// and the event. The form posts the selection as JSON in <paramref name="payload"/> so the antiforgery
        /// token travels as a normal form field.
        /// </summary>
        [HttpPost("Create")]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(string payload)
        {
            var email = CurrentUser();
            if (email == null) return Denied(json: true);
            try
            {
                var req = string.IsNullOrWhiteSpace(payload) ? null : JsonConvert.DeserializeObject<WhatsAppCampaignCreateRequest>(payload);
                if (req == null) return Json(new { ok = false, message = "Nothing was posted." });
                req.Name = req.Name?.Trim();
                if (string.IsNullOrWhiteSpace(req.Name)) return Json(new { ok = false, message = "Give the campaign a name." });
                if (req.Name.Length > 200) req.Name = req.Name.Substring(0, 200);

                var picked = (req.Candidates ?? new List<WhatsAppCampaignPickerItem>())
                    .Where(c => c.CandidateId > 0 && !string.IsNullOrWhiteSpace(c.Email))
                    .GroupBy(c => c.CandidateId).Select(g => g.First()).ToList();
                if (picked.Count == 0) return Json(new { ok = false, message = "Tick at least one candidate." });

                var source = req.SourceType ?? PlivoCXCampaignSource.IRF;
                var purpose = WhatsAppCampaignPurpose.ForSource(source);
                if (purpose == WhatsAppCampaignPurpose.Confirm && picked.Any(p => !p.InterviewId.HasValue))
                    return Json(new { ok = false, message = "Every candidate of a confirmation campaign needs a meeting to confirm." });

                var campaign = new WhatsAppCampaign
                {
                    Name       = req.Name,
                    SourceType = source,
                    Purpose    = purpose,
                    EventName  = WhatsAppCampaignPurpose.EventFor(purpose),
                    Status     = WhatsAppCampaignStatus.Draft,
                    CreatedBy  = email
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
                if (id <= 0) return Json(new { ok = false, message = "The campaign could not be saved. Have the WhatsApp campaign tables been deployed?" });

                // The picker's facts are re-checked here, not trusted from the post.
                var checkedRows = (await _campaignService.AnnotateAsync(picked.Select(p => new WhatsAppCampaignPickerItem
                {
                    CandidateId = p.CandidateId, Email = p.Email, IrfId = p.IrfId, InterviewId = p.InterviewId, Eligibility = "eligible"
                }), purpose)).ToDictionary(r => r.CandidateId);

                var rows = picked.Select(p =>
                {
                    checkedRows.TryGetValue(p.CandidateId, out var f);
                    var row = new WhatsAppCampaignCandidate
                    {
                        CandidateId           = p.CandidateId,
                        CandidateName         = Cut(p.Name, 200),
                        CandidateEmail        = Cut(p.Email?.Trim(), 200),
                        Phone                 = Cut(f?.Phone, 30),
                        IRFId                 = p.IrfId,
                        IRFName               = Cut(p.IrfName, 100),
                        JobTitle              = Cut(p.JobTitle, 255),
                        ClientName            = Cut(p.ClientName, 255),
                        InterviewId           = p.InterviewId,
                        OriginalInterviewDate = Cut(p.OriginalDate, 50),
                        State                 = WhatsAppCampaignCandidateState.Pending
                    };
                    // Ticked anyway: they join settled, with the reason, and are never sent to.
                    if (f?.Eligibility == "no-number") { row.State = WhatsAppCampaignCandidateState.NoNumber; row.OutcomeNote = "No WhatsApp number on file"; }
                    else if (f?.Eligibility == "not-eligible") { row.State = WhatsAppCampaignCandidateState.NoResponse; row.OutcomeNote = Cut(f.Note, 300); }
                    return row;
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

            // Settle who booked or confirmed before showing the table.
            await _campaignRepository.RefreshOutcomesAsync(id);
            var vm = new WhatsAppCampaignDetailVM
            {
                Campaign         = campaign,
                Candidates       = await _campaignRepository.GetCampaignCandidatesAsync(id),
                TimeZone         = PlivoCXCampaignRules.ResolveTimeZone(campaign.TimeZone),
                SchedulerEnabled = _campaignService.SchedulerEnabled
            };
            return View(vm);
        }

        /// <summary>Live tiles for the detail page, polled every 30 seconds while the campaign runs.</summary>
        [HttpGet("{id:int}/Counts")]
        public async Task<IActionResult> Counts(int id)
        {
            if (CurrentUser() == null) return Denied(json: true);
            var campaign = await _campaignRepository.GetCampaignAsync(id);
            if (campaign == null) return Json(new { ok = false, message = "Campaign not found." });
            await _campaignRepository.RefreshOutcomesAsync(id);
            var list = await _campaignRepository.GetCampaignCandidatesAsync(id);
            return Json(new
            {
                ok         = true,
                status     = campaign.Status,
                pending    = list.Count(c => c.State == WhatsAppCampaignCandidateState.Pending),
                sent       = list.Count(c => c.State == WhatsAppCampaignCandidateState.Sent),
                converted  = list.Count(c => c.State == WhatsAppCampaignCandidateState.Converted),
                declined   = list.Count(c => c.State == WhatsAppCampaignCandidateState.Declined),
                noResponse = list.Count(c => c.State == WhatsAppCampaignCandidateState.NoResponse),
                noNumber   = list.Count(c => c.State == WhatsAppCampaignCandidateState.NoNumber),
                failed     = list.Count(c => c.State == WhatsAppCampaignCandidateState.Failed),
                total      = list.Count
            });
        }

        [HttpPost("{id:int}/SavePolicy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePolicy(int id, string windowOpens, string windowCloses, string timeZone,
            int maxSends, int resendAfterHours, int batchSize, DateTime? startsOn, DateTime? endsOn)
        {
            if (CurrentUser() == null) return Denied(json: true);
            var campaign = await _campaignRepository.GetCampaignAsync(id);
            if (campaign == null) return Json(new { ok = false, message = "Campaign not found." });

            if (!TimeSpan.TryParseExact(windowOpens ?? "", @"hh\:mm", CultureInfo.InvariantCulture, out var opens)
                || !TimeSpan.TryParseExact(windowCloses ?? "", @"hh\:mm", CultureInfo.InvariantCulture, out var closes))
                return Json(new { ok = false, message = "Window times must be HH:mm." });

            campaign.WindowOpens      = opens;
            campaign.WindowCloses     = closes;
            campaign.TimeZone         = string.IsNullOrWhiteSpace(timeZone) ? PlivoCXCampaignRules.IndiaTimeZoneId : timeZone.Trim();
            campaign.MaxSends         = maxSends;
            campaign.ResendAfterHours = resendAfterHours;
            campaign.BatchSize        = batchSize;
            campaign.StartsOn         = startsOn?.Date;
            campaign.EndsOn           = endsOn?.Date;

            var error = WhatsAppCampaignRules.ValidatePolicy(campaign);
            if (error != null) return Json(new { ok = false, message = error });

            try
            {
                await _campaignRepository.UpdateCampaignPolicyAsync(campaign);
                return Json(new { ok = true, message = "Policy saved. It applies from the next send." });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "The policy could not be saved." });
            }
        }

        /// <summary>Sends to the due candidates once now, up to the batch size - ignoring the window and dates.</summary>
        [HttpPost("{id:int}/SendNow")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendNow(int id)
        {
            var email = CurrentUser();
            if (email == null) return Denied(json: true);
            var campaign = await _campaignRepository.GetCampaignAsync(id);
            if (campaign == null) return Json(new { ok = false, message = "Campaign not found." });
            if (campaign.Status == WhatsAppCampaignStatus.Paused)
                return Json(new { ok = false, message = "The campaign is paused. Start it again first." });
            if (campaign.Status == WhatsAppCampaignStatus.Completed)
                return Json(new { ok = false, message = "The campaign is completed." });

            try
            {
                var r = await _campaignService.SendDueAsync(campaign, email, respectWindow: false);
                return Json(new { ok = true, sent = r.Sent, skipped = r.Skipped, failed = r.Failed, remaining = r.Remaining, message = r.Message });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Sending stopped: " + ex.Message });
            }
        }

        [HttpPost("{id:int}/StartAutomatic")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> StartAutomatic(int id) => SetStatus(id, WhatsAppCampaignStatus.Running);

        [HttpPost("{id:int}/Pause")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Pause(int id) => SetStatus(id, WhatsAppCampaignStatus.Paused);

        private async Task<IActionResult> SetStatus(int id, string status)
        {
            if (CurrentUser() == null) return Denied(json: true);
            var campaign = await _campaignRepository.GetCampaignAsync(id);
            if (campaign == null) return Json(new { ok = false, message = "Campaign not found." });
            if (status == WhatsAppCampaignStatus.Running && await _campaignRepository.CountOpenAsync(id) == 0)
                return Json(new { ok = false, message = "Nobody is pending or waiting for a re-send - there is nothing to send." });

            await _campaignRepository.SetCampaignStatusAsync(id, status);
            string message;
            if (status == WhatsAppCampaignStatus.Paused)
                message = "Paused. Nothing more is sent until you start it again.";
            else if (_campaignService.SchedulerEnabled)
                message = "Started. The scheduler sends inside the window every 5 minutes, up to " + campaign.BatchSize + " at a time, and re-sends after " + campaign.ResendAfterHours + " hours.";
            else
                message = "Marked Running, but automatic sending is switched off (WhatsAppCampaign:SchedulerEnabled). Use Send now until it is turned on.";
            return Json(new { ok = true, status, message });
        }

        /// <summary>Sends the campaign's template to one team member (no candidate row changes).</summary>
        [HttpPost("{id:int}/TestSend")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestSend(int id, string email)
        {
            var user = CurrentUser();
            if (user == null) return Denied(json: true);
            var campaign = await _campaignRepository.GetCampaignAsync(id);
            if (campaign == null) return Json(new { ok = false, message = "Campaign not found." });

            var result = await _campaignService.TestSendAsync(campaign, email, user);
            bool sent = result == Risebird.CommonHelper.WhatsAppHelper.SentMessage;
            return Json(new
            {
                ok = sent,
                message = sent
                    ? "Test message sent to " + email + "'s WhatsApp number. Its buttons open a test page, not a real interview."
                    : "Not sent: " + result + (result == Risebird.CommonHelper.WhatsAppHelper.SkippedNoNumber ? " (the address must belong to a Risebird user with a number on their profile)" : "")
            });
        }

        /// <summary>Re-sends to one candidate row now (puts a settled row back to Pending first). Limits still apply.</summary>
        [HttpPost("SendCandidate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCandidate(int campaignId, int candidateId)
        {
            var user = CurrentUser();
            if (user == null) return Denied(json: true);
            var campaign = await _campaignRepository.GetCampaignAsync(campaignId);
            var cc = await _campaignRepository.GetCampaignCandidateAsync(campaignId, candidateId);
            if (campaign == null || cc == null) return Json(new { ok = false, message = "Candidate not found in this campaign." });

            if (cc.State != WhatsAppCampaignCandidateState.Pending)
                await _campaignRepository.SetCampaignCandidateStateAsync(cc.Id, WhatsAppCampaignCandidateState.Pending, "Re-send by " + user);

            var r = await _campaignService.SendCandidateAsync(campaign, cc, user);
            return Json(new { ok = r.Sent == 1, message = r.Message });
        }

        /// <summary>Manual state override from the Mark… menu.</summary>
        [HttpPost("MarkCandidate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkCandidate(int campaignId, int candidateId, string state)
        {
            var user = CurrentUser();
            if (user == null) return Denied(json: true);
            if (!WhatsAppCampaignCandidateState.IsValid(state)) return Json(new { ok = false, message = "Unknown state." });
            var campaign = await _campaignRepository.GetCampaignAsync(campaignId);
            var cc = await _campaignRepository.GetCampaignCandidateAsync(campaignId, candidateId);
            if (campaign == null || cc == null) return Json(new { ok = false, message = "Candidate not found in this campaign." });

            await _campaignRepository.SetCampaignCandidateStateAsync(cc.Id, state, "Marked by " + user);
            return Json(new { ok = true, message = (cc.CandidateName ?? "Candidate") + " marked " + WhatsAppCampaignCandidateState.Label(state, campaign.Purpose) + "." });
        }

        private static string Cut(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max);
    }
}
