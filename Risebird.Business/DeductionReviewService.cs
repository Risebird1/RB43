using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class DeductionReviewService : IDeductionReviewService
    {
        private const string SaveErrorMessage = "Could not save. Please try again.";

        private readonly IDeductionReviewRepository _repository;
        private readonly INotificationHelper _notificationHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly Func<DateTime> _utcNow;

        public DeductionReviewService(IDeductionReviewRepository repository, INotificationHelper notificationHelper,
            IApplicationErrorLogger applicationErrorLogger, DeductionReviewSettings settings)
            : this(repository, notificationHelper, applicationErrorLogger, settings, () => DateTime.UtcNow)
        {
        }

        /// <summary>Test seam for the clock. Internal so DI only sees the public constructor.</summary>
        internal DeductionReviewService(IDeductionReviewRepository repository, INotificationHelper notificationHelper,
            IApplicationErrorLogger applicationErrorLogger, DeductionReviewSettings settings, Func<DateTime> utcNow)
        {
            _repository = repository;
            _notificationHelper = notificationHelper;
            _applicationErrorLogger = applicationErrorLogger;
            Settings = settings ?? new DeductionReviewSettings();
            _utcNow = utcNow;
        }

        public DeductionReviewSettings Settings { get; }

        // ---------------- Interviewer side ----------------

        public async Task<CoachingActionResultVM> RequestReviewAsync(string sessionEmail, DeductionReviewRequestInputVM input)
        {
            var email = Clean(sessionEmail);
            if (email == null || input == null || input.InterviewId <= 0)
                return Fail("Deduction not found.");

            var reason = DeductionReviewConstants.CanonicalKey(DeductionReviewConstants.ReasonCategories, input.ReasonCategory);
            if (reason == null)
                return Fail("Pick a reason.");

            var note = input.Note?.Trim();
            if (string.IsNullOrEmpty(note) || note.Length < Settings.NoteMinLength)
                return Fail($"Please add a note of at least {Settings.NoteMinLength} characters.");
            if (note.Length > Settings.NoteMaxLength)
                return Fail($"Please keep the note under {Settings.NoteMaxLength} characters.");

            try
            {
                var now = _utcNow();
                var windowStart = now.AddMinutes(330).AddDays(-Settings.WindowDays);
                var due = DeductionReviewConstants.AddWorkingDaysUtc(now, Settings.ReplyWorkingDays);
                var (result, id) = await _repository.SaveRequestAsync(email, input.InterviewId, reason, note, Settings, windowStart, due);
                return result switch
                {
                    1 => new CoachingActionResultVM
                    {
                        Ok = true,
                        SessionId = id,
                        Message = "Review requested. You will get a reply by " + DayLabel(due) + "."
                    },
                    -2 => Fail($"Only deductions of {Settings.MinDeductionPct}% or more can be reviewed."),
                    -3 => Fail($"Reviews can be requested within {Settings.WindowDays} days of the deduction."),
                    -4 => Fail($"You have used your {Settings.MonthlyCap} review requests for this month."),
                    -5 => Fail("This deduction has already been sent for review."),
                    -6 => Fail("This deduction has not been applied yet."),
                    -7 => Fail("Review requests are not available yet."),
                    -8 => Fail("Pick a reason and add a note."),
                    _ => Fail("Deduction not found.")
                };
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Fail(SaveErrorMessage);
            }
        }

        // ---------------- QC side ----------------

        public async Task<DeductionReviewQueueVM> GetQueueAsync(bool canManagePolicy)
        {
            var vm = new DeductionReviewQueueVM { Settings = Settings, CanManagePolicy = canManagePolicy };
            try
            {
                await _repository.ExpireAsync(false);
                var (open, decided, checks) = await _repository.GetQueueAsync(30);
                var now = _utcNow();
                foreach (var r in open.Concat(decided))
                {
                    r.ReasonLabel = DeductionReviewConstants.Label(DeductionReviewConstants.ReasonCategories, r.ReasonCategory);
                    r.AiCheckLabel = DeductionReviewConstants.Label(DeductionReviewConstants.AiChecks, r.AiCheck);
                    r.IsOverdue = r.Status == DeductionReviewConstants.StatusOpen && r.DueAtUtc < now;
                }
                vm.Open = open;
                vm.RecentDecided = decided;
                vm.Checks = BuildCheckStats(checks, Settings);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                vm.Error = "Could not load review requests. Please try again.";
            }
            return vm;
        }

        public async Task<CoachingActionResultVM> DecideAsync(string qcEmail, DeductionReviewDecisionInputVM input)
        {
            var email = Clean(qcEmail);
            if (email == null || input == null || input.RequestId <= 0)
                return Fail("Request not found.");

            var decision = new[] { DeductionReviewConstants.DecisionStands, DeductionReviewConstants.DecisionReduce, DeductionReviewConstants.DecisionRefund }
                .FirstOrDefault(d => string.Equals(d, input.Decision?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (decision == null)
                return Fail("Pick Stands, Reduce or Refund.");

            var note = input.Note?.Trim();
            if (string.IsNullOrEmpty(note) || note.Length < DeductionReviewConstants.DecisionNoteMinLength)
                return Fail("Add a short reason for the interviewer.");
            if (note.Length > DeductionReviewConstants.DecisionNoteMaxLength)
                return Fail($"Keep the reason under {DeductionReviewConstants.DecisionNoteMaxLength} characters.");

            if (decision == DeductionReviewConstants.DecisionReduce && (!input.NewDeductionPct.HasValue || input.NewDeductionPct < 1))
                return Fail("Enter the new deduction %.");

            try
            {
                var r = await _repository.DecideAsync(input.RequestId, decision,
                    decision == DeductionReviewConstants.DecisionReduce ? input.NewDeductionPct : null, note, email);
                switch (r.Result)
                {
                    case 1:
                        var msg = r.Status == DeductionReviewConstants.StatusStands ? "Saved: the deduction stands."
                            : r.Status == DeductionReviewConstants.StatusRefunded ? "Saved: the deduction is refunded."
                            : $"Saved: the deduction is reduced to {r.NewDeductionPct}%.";
                        if (r.PayoutAlreadyPaid && r.Status != DeductionReviewConstants.StatusStands)
                            msg += " The interviewer was already paid for this interview, so the payout was not changed: ask Finance to settle the difference.";
                        return new CoachingActionResultVM { Ok = true, Message = msg, SessionId = input.RequestId };
                    case -2:
                        return Fail("This request has already been decided.");
                    case -3:
                        return Fail("The new % must be at least 1 and below the current deduction.");
                    case -4:
                        return Fail("Review requests are not available yet.");
                    default:
                        return Fail("Request not found.");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Fail(SaveErrorMessage);
            }
        }

        public async Task<CoachingActionResultVM> SetCheckPolicyAsync(string aiCheck, bool exclude, string modifiedBy)
        {
            var key = DeductionReviewConstants.CanonicalKey(DeductionReviewConstants.AiChecks, aiCheck);
            if (key == null || Clean(modifiedBy) == null)
                return Fail("Unknown check.");
            try
            {
                await _repository.SetCheckPolicyAsync(key, exclude, Clean(modifiedBy));
                var label = DeductionReviewConstants.Label(DeductionReviewConstants.AiChecks, key);
                return new CoachingActionResultVM
                {
                    Ok = true,
                    Message = exclude
                        ? $"\"{label}\" is now out of auto-approval: these deductions wait for a person."
                        : $"\"{label}\" is back in auto-approval."
                };
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Fail(SaveErrorMessage);
            }
        }

        public async Task<int> ExpireAndNotifyLeadAsync()
        {
            var expired = await _repository.ExpireAsync(true);
            if (expired == null || expired.Count == 0)
                return 0;

            var leads = (Settings.QcLeadEmails ?? "")
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => e.Contains('@'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (leads.Count == 0)
            {
                // No lead configured: the requests stay flagged (LeadNotifiedAtUtc null) and show on the queue page.
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                    $"{expired.Count} deduction review request(s) expired unanswered; set DeductionReview:QcLeadEmails to email the QC lead.");
                return 0;
            }

            var (subject, html) = BuildLeadEmail(expired, Settings);
            var allSent = true;
            foreach (var lead in leads)
            {
                var result = await _notificationHelper.SendInterviewerCoachingEmail(lead, null, null, subject, html);
                if (result == null || result.IndexOf("fail", StringComparison.OrdinalIgnoreCase) >= 0)
                    allSent = false;
            }
            if (allSent)
                await _repository.MarkLeadNotifiedAsync(expired.Select(e => e.Id));
            return allSent ? expired.Count : 0;
        }

        // ---------------- Pure helpers (unit-tested) ----------------

        public static IReadOnlyList<DeductionReviewCheckStatVM> BuildCheckStats(IEnumerable<DeductionReviewCheckStatVM> rows, DeductionReviewSettings settings)
        {
            settings ??= new DeductionReviewSettings();
            return (rows ?? Enumerable.Empty<DeductionReviewCheckStatVM>())
                .Where(r => r != null && !string.IsNullOrWhiteSpace(r.AiCheck))
                .Select(r =>
                {
                    r.AiCheckLabel = DeductionReviewConstants.Label(DeductionReviewConstants.AiChecks, r.AiCheck);
                    r.UpheldRatePct = r.Decided > 0 ? (int?)(int)Math.Round(r.Upheld * 100.0 / r.Decided, MidpointRounding.AwayFromZero) : null;
                    r.SuggestExclude = !r.ExcludeFromAutoApprove
                                       && r.Decided >= settings.MinDecisionsForRate
                                       && r.UpheldRatePct >= settings.HighUpheldRatePct;
                    return r;
                })
                .OrderByDescending(r => r.SuggestExclude)
                .ThenByDescending(r => r.UpheldRatePct ?? -1)
                .ThenByDescending(r => r.Decided)
                .ToList();
        }

        public static (string Subject, string Html) BuildLeadEmail(IReadOnlyList<DeductionReviewExpiredRowVM> expired, DeductionReviewSettings settings)
        {
            string E(string s) => WebUtility.HtmlEncode(s ?? "");
            var n = expired.Count;
            var subject = $"{n} deduction review request{(n == 1 ? "" : "s")} closed without a QC reply";
            var html = new StringBuilder();
            html.Append("<div style=\"font-family:Arial,Helvetica,sans-serif;font-size:14px;line-height:1.5;color:#0f172a\">");
            html.Append("<p>These interviewer review requests got no QC decision within ")
                .Append(settings?.ReplyWorkingDays ?? 5)
                .Append(" working days. They are now closed as <b>deduction stands</b>.</p>");
            html.Append("<table cellpadding=\"6\" style=\"border-collapse:collapse;font-size:13px\">")
                .Append("<tr style=\"background:#f1f5f9\"><th align=\"left\">Interview</th><th align=\"left\">Interviewer</th><th align=\"left\">Channel</th>")
                .Append("<th align=\"right\">Deduction</th><th align=\"left\">AI check</th><th align=\"left\">Requested</th><th align=\"left\">Due</th></tr>");
            foreach (var r in expired)
            {
                html.Append("<tr style=\"border-top:1px solid #e2e8f0\">")
                    .Append("<td>#").Append(r.InterviewId).Append("</td>")
                    .Append("<td>").Append(E(r.InterviewerEmail)).Append("</td>")
                    .Append("<td>").Append(E(r.IrfName ?? "—")).Append("</td>")
                    .Append("<td align=\"right\">").Append(r.DeductionPct).Append("%</td>")
                    .Append("<td>").Append(E(DeductionReviewConstants.Label(DeductionReviewConstants.AiChecks, r.AiCheck))).Append("</td>")
                    .Append("<td>").Append(E(DayLabel(r.RequestedAtUtc))).Append("</td>")
                    .Append("<td>").Append(E(DayLabel(r.DueAtUtc))).Append("</td>")
                    .Append("</tr>");
            }
            html.Append("</table><p>Open QC Activity → Review requests to see them.</p></div>");
            return (subject, html.ToString());
        }

        public static string DayLabel(DateTime utc) =>
            utc.AddMinutes(330).ToString("d MMM", CultureInfo.InvariantCulture);

        private static string Clean(string email) =>
            string.IsNullOrWhiteSpace(email) ? null : email.Trim().Replace("\"", "");

        private static CoachingActionResultVM Fail(string message) => new CoachingActionResultVM { Ok = false, Message = message };
    }
}
