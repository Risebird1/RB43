using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.Business.PlivoCX;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.WhatsAppCampaignModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business.WhatsAppCampaigns
{
    /// <summary>
    /// The WhatsApp campaign sending engine (hand-off: WhatsApp Campaign pages). Every send goes through the
    /// same path as the single sends - IRFService.SendWhatsAppInvite for C1 / C2, WhatsAppHelper.
    /// WhatsAppSendInterviewReminder for C3 - so the WhatsApp opt-out and the 3-per-IRF limit apply, and each
    /// attempt is recorded in SentWhatsAppMessageDetails as usual.
    /// </summary>
    public class WhatsAppCampaignService : IWhatsAppCampaignService
    {
        // How long a claimed row is held by one sender (covers a crash mid-send).
        private const int SendLockMinutes = 5;

        // Gap between two sends of one pass, to stay well inside Interakt's rate limit.
        private const int SendIntervalMs = 300;

        private readonly IConfiguration              _configuration;
        private readonly IWhatsAppCampaignRepository _campaignRepository;
        private readonly IMeetingRespository         _meetingRespository;
        private readonly IIRFService                 _irfService;
        private readonly IWhatsAppHelper             _whatsAppHelper;
        private readonly IApplicationErrorLogger     _logger;

        public WhatsAppCampaignService(
            IConfiguration              configuration,
            IWhatsAppCampaignRepository campaignRepository,
            IMeetingRespository         meetingRespository,
            IIRFService                 irfService,
            IWhatsAppHelper             whatsAppHelper,
            IApplicationErrorLogger     logger)
        {
            _configuration      = configuration;
            _campaignRepository = campaignRepository;
            _meetingRespository = meetingRespository;
            _irfService         = irfService;
            _whatsAppHelper     = whatsAppHelper;
            _logger             = logger;
        }

        public bool SchedulerEnabled => bool.TryParse(_configuration["WhatsAppCampaign:SchedulerEnabled"], out var on) && on;

        // The scheduler has no HTTP request; C3's "Yes! Available" button carries the full portal URL.
        private string PortalBaseUrl => _configuration["WhatsAppCampaign:PortalBaseUrl"] ?? "https://portal.risebird.io";

        public int SendLimitFor(string purpose) => purpose switch
        {
            WhatsAppCampaignPurpose.Schedule   => ConfigInt("WhatsApp:MaxNotScheduledSendsPerIrf", 3),
            WhatsAppCampaignPurpose.Reschedule => ConfigInt("WhatsApp:MaxRescheduleSendsPerIrf", 3),
            _                                  => 0
        };

        private int ConfigInt(string key, int fallback) =>
            int.TryParse(_configuration[key], out var value) && value > 0 ? value : fallback;

        // ── Sending ───────────────────────────────────────────────────────────────

        public async Task<WhatsAppCampaignSendResult> SendDueAsync(WhatsAppCampaign campaign, string actor, bool respectWindow)
        {
            await _campaignRepository.RefreshOutcomesAsync(campaign.Id);
            await _campaignRepository.AdvanceResendsAsync(campaign.Id);

            if (respectWindow)
            {
                var why = WhatsAppCampaignRules.WhyNotSending(campaign, DateTime.UtcNow, PlivoCXCampaignRules.ResolveTimeZone(campaign.TimeZone));
                if (why != null)
                    return new WhatsAppCampaignSendResult { Remaining = await _campaignRepository.CountPendingAsync(campaign.Id), Message = "Nothing sent: " + why + "." };
            }

            var claimed = await _campaignRepository.ClaimDueCandidatesAsync(campaign.Id, Math.Clamp(campaign.BatchSize, 1, 200), SendLockMinutes);
            var result = await SendClaimedAsync(campaign, claimed, actor);
            result.Remaining = await _campaignRepository.CountPendingAsync(campaign.Id);
            result.Message = claimed.Count == 0
                ? (result.Remaining == 0 ? "Nobody is due - every candidate has been sent to or settled." : "Nobody is due yet: the pending candidates are waiting for their re-send time.")
                : Summary(result);
            return result;
        }

        public async Task<WhatsAppCampaignSendResult> SendCandidateAsync(WhatsAppCampaign campaign, WhatsAppCampaignCandidate row, string actor)
        {
            var claimed = await _campaignRepository.ClaimDueCandidatesAsync(campaign.Id, 1, SendLockMinutes, row.Id);
            if (claimed.Count == 0)
                return new WhatsAppCampaignSendResult { Message = "A send to " + (row.CandidateName ?? "this candidate") + " is already in progress." };

            var result = await SendClaimedAsync(campaign, claimed, actor);
            var after = await _campaignRepository.GetCampaignCandidateAsync(campaign.Id, row.CandidateId);
            result.Message = (row.CandidateName ?? "Candidate") + ": " + (result.Sent == 1
                ? "sent."
                : WhatsAppCampaignCandidateState.Label(after?.State, campaign.Purpose) + (string.IsNullOrWhiteSpace(after?.OutcomeNote) ? "" : " - " + after.OutcomeNote) + ".");
            return result;
        }

        private static string Summary(WhatsAppCampaignSendResult r) =>
            "Sent " + r.Sent + " · skipped " + r.Skipped + " · failed " + r.Failed + (r.Remaining > 0 ? " · " + r.Remaining + " still pending" : "");

        private async Task<WhatsAppCampaignSendResult> SendClaimedAsync(WhatsAppCampaign campaign, List<WhatsAppCampaignCandidate> rows, string actor)
        {
            var result = new WhatsAppCampaignSendResult();
            if (rows.Count == 0) return result;

            // C3 needs the meeting's reminder details; one query for the whole batch.
            var meetings = new Dictionary<int, UnassignedInterviewVM>();
            if (campaign.Purpose == WhatsAppCampaignPurpose.Confirm)
            {
                var found = await _meetingRespository.FetchCandidateMeetingsForManualReminder(rows.Where(r => r.InterviewId.HasValue).Select(r => r.InterviewId.Value).ToList());
                foreach (var m in found) meetings[m.Interviewid] = m;
            }

            bool first = true;
            foreach (var row in rows)
            {
                if (!first) await Task.Delay(SendIntervalMs);
                first = false;

                string sendResult;
                string error = null;
                try
                {
                    sendResult = await SendRowAsync(campaign, row, meetings, actor);
                }
                catch (Exception ex)
                {
                    await _logger.LogMessage(LogLevelInfo.Exception, ex);
                    sendResult = WhatsAppHelper.FailedMessage;
                    error = ex.Message;
                }

                var transition = WhatsAppCampaignRules.AfterSend(sendResult, row.FailedSends, DateTime.UtcNow);
                if (transition.State == WhatsAppCampaignCandidateState.Pending || transition.State == WhatsAppCampaignCandidateState.Failed)
                    error ??= "Interakt did not accept the message; the reason is in the application log (Risebirdlogs).";
                await _campaignRepository.SaveSendResultAsync(row.Id, transition, sendResult, error);

                if (transition.Counted) result.Sent++;
                else if (transition.State == WhatsAppCampaignCandidateState.Pending || transition.State == WhatsAppCampaignCandidateState.Failed) result.Failed++;
                else result.Skipped++;
            }
            return result;
        }

        private async Task<string> SendRowAsync(WhatsAppCampaign campaign, WhatsAppCampaignCandidate row, Dictionary<int, UnassignedInterviewVM> meetings, string actor)
        {
            if (string.IsNullOrWhiteSpace(row.CandidateEmail))
                return WhatsAppHelper.SkippedNoNumber;

            if (campaign.Purpose == WhatsAppCampaignPurpose.Confirm)
            {
                if (!row.InterviewId.HasValue || !meetings.TryGetValue(row.InterviewId.Value, out var meeting))
                    return "Skipped: the interview is no longer awaiting confirmation";
                return await _whatsAppHelper.WhatsAppSendInterviewReminder(CandidateReminderLinks.For(meeting, PortalBaseUrl).WhatsApp(meeting, actor));
            }

            if (!row.IRFId.HasValue || row.IRFId.Value <= 0)
                return "Skipped: no IRF on the row";
            return await _irfService.SendWhatsAppInvite(row.IRFId.Value, row.CandidateEmail, campaign.EventName, actor);
        }

        public async Task<string> TestSendAsync(WhatsAppCampaign campaign, string email, string actor)
        {
            if (string.IsNullOrWhiteSpace(email)) return "Enter the email of a team member whose WhatsApp number is on their Risebird profile.";
            var sample = (await _campaignRepository.GetCampaignCandidatesAsync(campaign.Id)).FirstOrDefault();

            // No IRF and no interview on a test: it does not count toward anyone's limit, and its buttons open a
            // harmless page instead of booking or confirming a real interview.
            if (campaign.Purpose == WhatsAppCampaignPurpose.Confirm)
            {
                return await _whatsAppHelper.WhatsAppSendInterviewReminder(new WhatsAppVM
                {
                    Emailid       = email.Trim(),
                    InterviewDate = DateTime.Now.AddDays(1).Date.AddHours(11).ToString(),
                    buttonlink    = "Candidate/VerifiedInterviewDetails/test",
                    DashboardLink = "Candidate/VerifiedInterviewDetails/test",
                    Event         = campaign.EventName,
                    PrimarySkill  = sample?.JobTitle ?? "Test role",
                    ClientName    = sample?.ClientName ?? "Test client",
                    timezone      = "IST",
                    Header        = "Test send",
                    SentBy        = actor
                });
            }
            return await _whatsAppHelper.WhatsAppSendScheduling(new WhatsAppVM
            {
                Emailid      = email.Trim(),
                Event        = campaign.EventName,
                ClientName   = sample?.ClientName ?? "Test client",
                PrimarySkill = sample?.JobTitle ?? "Test role",
                buttonlink   = "InterviewScheduler/CandidateScheduleEvent?id=test",
                Header       = "Test send",
                SentBy       = actor
            });
        }

        // ── Create picker ─────────────────────────────────────────────────────────

        public async Task<List<WhatsAppCampaignPickerItem>> AnnotateAsync(IEnumerable<WhatsAppCampaignPickerItem> items, string purpose)
        {
            var list = items.ToList();
            var keys = list.Where(i => i.CandidateId > 0 && !string.IsNullOrWhiteSpace(i.Email))
                           .Select(i => new WhatsAppCampaignEligibilityRow { CandidateId = i.CandidateId, Email = i.Email, IRFId = i.IrfId });
            var facts = (await _campaignRepository.GetEligibilityAsync(keys, WhatsAppCampaignPurpose.EventFor(purpose)))
                .GroupBy(f => f.CandidateId).ToDictionary(g => g.Key, g => g.First());
            int limit = SendLimitFor(purpose);

            foreach (var item in list)
            {
                item.SendLimit = limit;
                // The source already ruled it out (no candidate record, no meeting to confirm, ...).
                if (item.CandidateId <= 0 || item.Eligibility == "not-eligible") { item.Selected = false; continue; }

                facts.TryGetValue(item.CandidateId, out var f);
                item.Phone     = f?.WhatsAppNumber?.Trim();
                item.SendsUsed = f?.SendsUsed ?? 0;
                item.OptedOut  = f?.OptedOut ?? false;

                var (eligibility, note) = WhatsAppCampaignRules.Eligibility(f, purpose, limit);
                // A candidate the source marked as holding a meeting stays "has meeting" (unticked) when otherwise eligible.
                item.Eligibility = eligibility == "eligible" && item.Eligibility == "has-meeting" ? "has-meeting" : eligibility;
                item.Note = JoinNotes(item.Note, note);
                if (!string.IsNullOrWhiteSpace(f?.ActiveCampaignName))
                    item.Note = JoinNotes(item.Note, "in WhatsApp campaign \"" + f.ActiveCampaignName + "\"");
                item.Selected = item.Eligibility == "eligible";
            }
            return list;
        }

        private static string JoinNotes(string a, string b) =>
            string.IsNullOrWhiteSpace(a) ? b : string.IsNullOrWhiteSpace(b) ? a : a + " · " + b;

        // ── Scheduler ─────────────────────────────────────────────────────────────

        public async Task RunSchedulerTickAsync()
        {
            if (!SchedulerEnabled) return;
            foreach (var campaign in await _campaignRepository.GetRunningCampaignsAsync())
            {
                try
                {
                    var tz = PlivoCXCampaignRules.ResolveTimeZone(campaign.TimeZone);
                    if (WhatsAppCampaignRules.HasEnded(campaign, DateTime.UtcNow, tz))
                    {
                        await _campaignRepository.SetCampaignStatusAsync(campaign.Id, WhatsAppCampaignStatus.Completed);
                        continue;
                    }
                    await SendDueAsync(campaign, "WhatsAppCampaignScheduler", respectWindow: true);
                    if (await _campaignRepository.CountOpenAsync(campaign.Id) == 0)
                        await _campaignRepository.SetCampaignStatusAsync(campaign.Id, WhatsAppCampaignStatus.Completed);
                }
                catch (Exception ex)
                {
                    await _logger.LogMessage(LogLevelInfo.Exception, ex);
                }
            }
        }
    }
}
