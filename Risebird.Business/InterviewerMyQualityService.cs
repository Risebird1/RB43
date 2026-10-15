using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    /// <summary>
    /// Builds the interviewer's own My Quality page. Everything is worded as behaviour: no thresholds,
    /// no internal labels (P0-P5, High-Risk, Remove / Watch), no fraud or proxy signals, no QC names.
    /// <see cref="Build"/> is pure so it is unit-tested directly.
    /// </summary>
    public class InterviewerMyQualityService : IInterviewerMyQualityService
    {
        private const string LoadErrorMessage = "Could not load your quality summary. Please try again later.";
        public const int TopFocusCount = 3;
        public const int MaxEvidencePerFocus = 5;

        /// <summary>
        /// Code defaults for every theme key used by InterviewerCoachingBriefBuilder. A row in
        /// dbo.InterviewerFeedbackText overrides the wording without a deploy.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, (string Title, string WhatToDo)> DefaultWording =
            new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
            {
                [InterviewerCoachingBriefBuilder.ThemeReportQuality] = ("Write fuller feedback when you select / reject", "Name the skills tested, the answers, and why."),
                [InterviewerCoachingBriefBuilder.ThemeJoinOnTime] = ("Join every interview on time", "Join 2 minutes early; message the candidate if you are running late."),
                [InterviewerCoachingBriefBuilder.ThemeQuestions] = ("Cover more ground in each interview", "Use the question set and log every question you ask."),
                [InterviewerCoachingBriefBuilder.ThemeInterviewTime] = ("Run the full interview length", "Plan the time and do not end early."),
                [InterviewerCoachingBriefBuilder.ThemeCameraProctoring] = ("Keep your camera on", "Camera on for the whole interview."),
                [InterviewerCoachingBriefBuilder.ThemeCandidateExperience] = ("Candidates rate the experience low", "Introduce yourself, explain the format, stay courteous."),
                [InterviewerCoachingBriefBuilder.ThemeRubricDecision] = ("Match your decisions to the evidence", "Decide on the skills tested, not the overall impression."),
                [InterviewerCoachingBriefBuilder.ThemeRedo] = ("Run interviews that stand on their own", "Check your setup and cover every skill, so no second interview is needed."),
                [InterviewerCoachingBriefBuilder.ThemeSetup] = ("Have a reliable setup", "Test your connection, room and headset before the interview starts."),
                [InterviewerCoachingBriefBuilder.ThemeConduct] = ("Keep every interview fair and professional", "Ask job-related questions only and stay neutral."),
                [InterviewerCoachingBriefBuilder.ThemeOther] = ("Other review findings", "Your coach will go through these with you."),
                [InterviewerCoachingBriefBuilder.ThemeChannelStanding] = ("Channel standing", "Your coach will tell you about your channels.")
            };

        /// <summary>Themes never shown to the interviewer (internal standing, unknown codes, conduct / fraud-adjacent).</summary>
        public static readonly HashSet<string> NeverShownThemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            InterviewerCoachingBriefBuilder.ThemeChannelStanding,
            InterviewerCoachingBriefBuilder.ThemeOther,
            InterviewerCoachingBriefBuilder.ThemeConduct
        };

        /// <summary>Fake-report / no-show checks: handled by QC by call, never shown.</summary>
        private static readonly HashSet<string> NeverShownChecks = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Interviewer No Show", "No Show", "Candidate No Show"
        };

        private static readonly string[] NeverShownWords = { "suspicious", "proxy", "fake", "fraud", "kyc", "identity", "impersonat" };

        private readonly IInterviewerMyQualityRepository _repository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly Func<DateTime> _utcNow;
        private readonly DeductionReviewSettings _reviewSettings;

        public InterviewerMyQualityService(IInterviewerMyQualityRepository repository, IApplicationErrorLogger applicationErrorLogger,
            DeductionReviewSettings reviewSettings)
            : this(repository, applicationErrorLogger, () => DateTime.UtcNow, reviewSettings)
        {
        }

        /// <summary>Test seam for the clock. Internal so DI only sees the public constructor.</summary>
        internal InterviewerMyQualityService(IInterviewerMyQualityRepository repository, IApplicationErrorLogger applicationErrorLogger,
            Func<DateTime> utcNow, DeductionReviewSettings reviewSettings = null)
        {
            _repository = repository;
            _applicationErrorLogger = applicationErrorLogger;
            _utcNow = utcNow;
            _reviewSettings = reviewSettings ?? new DeductionReviewSettings();
        }

        public async Task<InterviewerMyQualityVM> GetAsync(string sessionEmail, InterviewerInsightsVM lifetime)
        {
            var email = string.IsNullOrWhiteSpace(sessionEmail) ? null : sessionEmail.Trim().Replace("\"", "");
            if (email == null)
                return new InterviewerMyQualityVM { Error = LoadErrorMessage };
            try
            {
                var data = await _repository.GetAsync(email);
                var vm = Build(data, _utcNow(), _reviewSettings);
                vm.Record = BuildRecord(lifetime);
                return vm;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new InterviewerMyQualityVM { Error = LoadErrorMessage, Record = BuildRecord(lifetime) };
            }
        }

        public async Task<CoachingActionResultVM> AcknowledgeCoachingAsync(int sessionId, string sessionEmail)
        {
            var email = string.IsNullOrWhiteSpace(sessionEmail) ? null : sessionEmail.Trim().Replace("\"", "");
            if (sessionId <= 0 || email == null)
                return new CoachingActionResultVM { Ok = false, Message = "Coaching session not found." };
            try
            {
                var result = await _repository.AcknowledgeCoachingAsync(sessionId, email);
                return result switch
                {
                    1 => new CoachingActionResultVM { Ok = true, Message = "Marked as read.", SessionId = sessionId },
                    0 => new CoachingActionResultVM { Ok = true, Message = "Already marked as read.", SessionId = sessionId },
                    -2 => new CoachingActionResultVM { Ok = false, Message = "This is not available yet." },
                    _ => new CoachingActionResultVM { Ok = false, Message = "Coaching session not found." }
                };
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new CoachingActionResultVM { Ok = false, Message = "Could not save. Please try again." };
            }
        }

        // ---------------- Pure building ----------------

        public static InterviewerMyQualityVM Build(InterviewerMyQualityDataVM data, DateTime nowUtc) =>
            Build(data, nowUtc, new DeductionReviewSettings());

        public static InterviewerMyQualityVM Build(InterviewerMyQualityDataVM data, DateTime nowUtc, DeductionReviewSettings reviewSettings)
        {
            reviewSettings ??= new DeductionReviewSettings();
            var wording = WordingMap(data?.Wording);
            var tiles = data?.Tiles ?? new MyQualityTilesRowVM();
            var todayIst = nowUtc.AddMinutes(330).Date;
            var curFromIst = todayIst.AddDays(-29);

            var vm = new InterviewerMyQualityVM
            {
                IssueFree = Tile(tiles.CurIssueFree, tiles.CurCompleted, tiles.PrevIssueFree, tiles.PrevCompleted, higherIsBetter: true),
                OnTime = Tile(tiles.CurOnTime, tiles.CurJoinTracked, tiles.PrevOnTime, tiles.PrevJoinTracked, higherIsBetter: true),
                MonthDeductionCount = tiles.MonthDeductionCount,
                MonthDeductionAvgPct = tiles.MonthDeductionAvgPct,
                MonthDeductionAmount = tiles.MonthDeductionAmount,
                DeductionTrend = tiles.MonthDeductionCount == 0 && tiles.PrevMonthDeductionCount == 0 ? null
                    : tiles.MonthDeductionCount < tiles.PrevMonthDeductionCount ? "Down"
                    : tiles.MonthDeductionCount > tiles.PrevMonthDeductionCount ? "Up" : "Same",
                ActiveChannels = tiles.ActiveChannels,
                PausedChannels = tiles.PausedChannels,
                ReviewSettings = reviewSettings,
                ReviewRequestsThisMonth = tiles.ReviewRequestsThisMonth
            };

            vm.TopFocus = RankFocus(data?.Issues, data?.Deductions, curFromIst, wording);

            vm.Deductions = (data?.Deductions ?? Array.Empty<MyQualityDeductionRowVM>())
                .Where(d => d != null && (d.PaymentDeduction.GetValueOrDefault() > 0 || !string.IsNullOrEmpty(d.ReviewStatus)))
                .OrderByDescending(d => d.InterviewDateIst)
                .Select(d => ApplyReviewState(new MyQualityDeductionVM
                {
                    InterviewId = d.InterviewId,
                    IrfId = d.IrfId,
                    IrfName = d.IrfName,
                    DateIst = Day(d.InterviewDateIst),
                    Percent = d.PaymentDeduction.GetValueOrDefault(),
                    Amount = d.ApprovalState == 1 ? d.Amount : null,
                    Reason = DeductionReason(d, wording),
                    Status = d.ApprovalState == 1 ? "Applied" : d.ApprovalState == 2 ? "Not applied" : "Being checked"
                }, d, nowUtc, reviewSettings, tiles.ReviewRequestsThisMonth))
                .ToList();

            vm.Channels = (data?.Channels ?? Array.Empty<MyQualityChannelRowVM>())
                .Where(c => c != null && c.Completed > 0)
                .Select(c => new MyQualityChannelVM
                {
                    IrfName = c.IrfName,
                    Completed = c.Completed,
                    IssueFree = c.IssueFree,
                    Percent = Percent(c.IssueFree, c.Completed),
                    Standing = string.Equals(c.Standing, "Paused", StringComparison.OrdinalIgnoreCase) ? "Paused"
                        : string.Equals(c.Standing, "Active", StringComparison.OrdinalIgnoreCase) ? "Active" : null
                })
                .ToList();

            vm.Coaching = (data?.Coaching ?? Array.Empty<MyQualityCoachingRowVM>())
                .Where(c => c != null && c.SessionId > 0)
                .Select(c => new MyQualityCoachingVM
                {
                    SessionId = c.SessionId,
                    DateIst = Day(c.CreatedAtUtc.AddMinutes(330)),
                    Channel = CoachingConstants.Channels.TryGetValue(c.Channel?.Trim() ?? "", out var label) ? label : "Other",
                    Topics = (c.Themes ?? "")
                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => t.Length > 0 && !NeverShownThemes.Contains(t) && DefaultWording.ContainsKey(t))
                        .Select(t => Words(t, wording).Title)
                        .Distinct()
                        .ToList(),
                    Acknowledged = c.AcknowledgedAtUtc.HasValue
                })
                .ToList();

            return vm;
        }

        /// <summary>
        /// Review state of one deduction: Request review when the rules allow it; otherwise the grey reason
        /// (below the minimum, older than the window, monthly limit); or the request outcome.
        /// </summary>
        public static MyQualityDeductionVM ApplyReviewState(MyQualityDeductionVM vm, MyQualityDeductionRowVM row, DateTime nowUtc,
            DeductionReviewSettings settings, int requestsThisMonth)
        {
            settings ??= new DeductionReviewSettings();
            var status = row?.ReviewStatus?.Trim();
            if (!string.IsNullOrEmpty(status))
            {
                vm.ReviewStatus = status;
                vm.CanRequestReview = false;
                vm.ReviewDecisionNote = string.IsNullOrWhiteSpace(row.ReviewDecisionNote) ? null : row.ReviewDecisionNote.Trim();
                vm.ReviewLabel = status switch
                {
                    DeductionReviewConstants.StatusOpen => "Under review · reply by " +
                        (row.ReviewDueAtUtc.HasValue ? DeductionReviewService.DayLabel(row.ReviewDueAtUtc.Value) : "soon"),
                    DeductionReviewConstants.StatusReduced => $"Reviewed · reduced to {row.ReviewNewPct ?? vm.Percent}%",
                    DeductionReviewConstants.StatusRefunded => "Reviewed · refunded",
                    _ => "Reviewed · deduction stands"
                };
                // Show the % as it was when the review was asked for; the label carries the outcome.
                if (row.ReviewOriginalPct.HasValue) vm.Percent = row.ReviewOriginalPct.Value;
                return vm;
            }

            if (row == null || row.ApprovalState != 1)
                return vm;
            if (vm.Percent < settings.MinDeductionPct)
            {
                vm.ReviewBlockedReason = $"Below {settings.MinDeductionPct}%";
                return vm;
            }
            var appliedAt = row.AppliedAt ?? row.InterviewDateIst;
            var nowIst = nowUtc.AddMinutes(330);
            if (appliedAt.HasValue && appliedAt.Value < nowIst.AddDays(-settings.WindowDays))
            {
                vm.ReviewBlockedReason = $"Older than {settings.WindowDays} days";
                return vm;
            }
            if (requestsThisMonth >= settings.MonthlyCap)
            {
                vm.ReviewBlockedReason = "Monthly limit reached";
                return vm;
            }
            vm.CanRequestReview = true;
            return vm;
        }

        /// <summary>Ranks themes by count x severity weight and keeps the top three. Never-shown checks and themes are dropped first.</summary>
        public static IReadOnlyList<MyQualityFocusVM> RankFocus(IEnumerable<ExposureRiskRowVM> issues, IEnumerable<MyQualityDeductionRowVM> deductions,
            DateTime curFromIst, IReadOnlyDictionary<string, InterviewerFeedbackTextVM> wording)
        {
            var hits = new List<(string Theme, string Severity, MyQualityEvidenceVM Evidence)>();

            foreach (var r in issues ?? Enumerable.Empty<ExposureRiskRowVM>())
            {
                if (r == null) continue;
                foreach (var (type, severity) in InterviewerCoachingBriefBuilder.ParseChecks(r.SeverityBreakdown, r.Description))
                {
                    if (IsNeverShownCheck(type)) continue;
                    if (!InterviewerCoachingBriefBuilder.TryGetRiskCheckTheme(type, out var theme)) continue;
                    if (NeverShownThemes.Contains(theme)) continue;
                    hits.Add((theme, severity ?? NormalizeSeverity(r.Severity), Evidence(r.InterviewId, r.IrfId, r.IrfName, r.InterviewDateIst)));
                }
            }

            foreach (var d in deductions ?? Enumerable.Empty<MyQualityDeductionRowVM>())
            {
                if (d == null || d.PaymentDeduction.GetValueOrDefault() <= 0) continue;
                if (d.InterviewDateIst.HasValue && d.InterviewDateIst.Value.Date < curFromIst.Date) continue;
                foreach (var theme in InterviewerCoachingBriefBuilder.PaymentReasonThemes(d))
                {
                    if (NeverShownThemes.Contains(theme)) continue;
                    hits.Add((theme, null, Evidence(d.InterviewId, d.IrfId, d.IrfName, d.InterviewDateIst)));
                }
            }

            var order = InterviewerCoachingBriefBuilder.ThemeKeys.ToList();
            return hits
                .GroupBy(h => h.Theme, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var evidence = g.Select(h => h.Evidence)
                        .GroupBy(e => e.InterviewId)
                        .Select(x => x.First())
                        .OrderByDescending(e => e.DateIst)
                        .ToList();
                    var worst = g.Select(h => SeverityWeight(h.Severity)).DefaultIfEmpty(1).Max();
                    var words = Words(g.Key, wording);
                    return new
                    {
                        Score = evidence.Count * worst,
                        Order = order.IndexOf(g.Key) < 0 ? int.MaxValue : order.IndexOf(g.Key),
                        Focus = new MyQualityFocusVM
                        {
                            ThemeKey = g.Key,
                            Title = words.Title,
                            WhatToDo = words.WhatToDo,
                            Count = evidence.Count,
                            Evidence = evidence.Take(MaxEvidencePerFocus).ToList()
                        }
                    };
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Focus.Count)
                .ThenBy(x => x.Order)
                .Take(TopFocusCount)
                .Select(x => x.Focus)
                .ToList();
        }

        public static MyQualityRecordVM BuildRecord(InterviewerInsightsVM lifetime)
        {
            if (lifetime == null) return null;
            return new MyQualityRecordVM
            {
                Completed = lifetime.CompletedInterviews,
                SelectPercent = Percent(lifetime.SelectedInterviews, lifetime.CompletedInterviews),
                RejectPercent = Percent(lifetime.RejectedInterviews, lifetime.CompletedInterviews),
                NoShows = lifetime.NoShowInterviews,
                LateJoins = lifetime.JoinLateMeeting
            };
        }

        public static IReadOnlyDictionary<string, InterviewerFeedbackTextVM> WordingMap(IEnumerable<InterviewerFeedbackTextVM> rows)
        {
            var map = new Dictionary<string, InterviewerFeedbackTextVM>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows ?? Enumerable.Empty<InterviewerFeedbackTextVM>())
            {
                if (r == null || string.IsNullOrWhiteSpace(r.ThemeKey)) continue;
                if (string.IsNullOrWhiteSpace(r.Title) || string.IsNullOrWhiteSpace(r.WhatToDo)) continue;
                map[r.ThemeKey.Trim()] = r;
            }
            return map;
        }

        public static (string Title, string WhatToDo) Words(string themeKey, IReadOnlyDictionary<string, InterviewerFeedbackTextVM> wording)
        {
            if (themeKey != null && wording != null && wording.TryGetValue(themeKey, out var row))
                return (row.Title.Trim(), row.WhatToDo.Trim());
            if (themeKey != null && DefaultWording.TryGetValue(themeKey, out var d))
                return d;
            return ("Review finding", "Your coach will go through this with you.");
        }

        /// <summary>
        /// Plain reason per Payment SLA reason column, in the order shown. Key = column name; an
        /// InterviewerFeedbackText row with ThemeKey "Reason.&lt;column&gt;" overrides the text (Title) without a deploy.
        /// UnethicalPractices is never shown (conduct / fraud-adjacent). The column's own text (QC or AI flag
        /// wording) is never shown either: it can carry thresholds and internal labels.
        /// </summary>
        public static readonly IReadOnlyList<(string Column, string Reason, Func<ExposurePaymentRowVM, string> Value)> DeductionReasonColumns =
            new (string, string, Func<ExposurePaymentRowVM, string>)[]
            {
                (nameof(ExposurePaymentRowVM.AttendanceJoinCompliance), "Joined the interview late", p => p.AttendanceJoinCompliance),
                (nameof(ExposurePaymentRowVM.PoorFeedbackComments), "Feedback comments were too brief", p => p.PoorFeedbackComments),
                (nameof(ExposurePaymentRowVM.TimeManagement), "Interview time was not used fully", p => p.TimeManagement),
                (nameof(ExposurePaymentRowVM.QuestionsRelevance), "Questions were not relevant enough to the role", p => p.QuestionsRelevance),
                (nameof(ExposurePaymentRowVM.LimitedHandoncoding), "Too little hands-on coding", p => p.LimitedHandoncoding),
                (nameof(ExposurePaymentRowVM.Inaccuratedecisionmaking), "Decision did not match the interview evidence", p => p.Inaccuratedecisionmaking),
                (nameof(ExposurePaymentRowVM.ScoreRubricIgnorance), "Scores were not given to the rubric", p => p.ScoreRubricIgnorance),
                (nameof(ExposurePaymentRowVM.WrongStatusUpdates), "Interview status was updated incorrectly", p => p.WrongStatusUpdates),
                (nameof(ExposurePaymentRowVM.InterviewerVideo), "Camera was off during the interview", p => p.InterviewerVideo),
                (nameof(ExposurePaymentRowVM.ProctoringIgnorance), "Proctoring alerts were not acted on", p => p.ProctoringIgnorance),
                (nameof(ExposurePaymentRowVM.CandidateInteractionQuality), "Interaction with the candidate", p => p.CandidateInteractionQuality),
                (nameof(ExposurePaymentRowVM.PoorCommunicationSkills), "Clarity of communication", p => p.PoorCommunicationSkills),
                (nameof(ExposurePaymentRowVM.WeakInfrastructure), "Connection or setup problems", p => p.WeakInfrastructure),
            };

        public const string GenericDeductionReason = "Report quality review";
        public const string RedoDeductionReason = "The interview needed to be redone";

        /// <summary>Plain reason for a deduction: what was found, never who saved it, the raw flag text or an internal label.</summary>
        public static string DeductionReason(MyQualityDeductionRowVM d, IReadOnlyDictionary<string, InterviewerFeedbackTextVM> wording)
        {
            if (d == null) return GenericDeductionReason;
            var reasons = DeductionReasonColumns
                .Where(c => InterviewerCoachingBriefBuilder.ReasonParts(c.Value(d)) != null)
                .Select(c => wording != null && wording.TryGetValue("Reason." + c.Column, out var w) && !string.IsNullOrWhiteSpace(w.Title)
                    ? w.Title.Trim()
                    : c.Reason)
                .ToList();
            if (string.Equals(d.Recommendation?.Trim(), "REDO", StringComparison.OrdinalIgnoreCase))
                reasons.Add(RedoDeductionReason);
            reasons = reasons.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return reasons.Count > 0 ? string.Join("; ", reasons) : GenericDeductionReason;
        }

        public static bool IsNeverShownCheck(string checkType)
        {
            if (string.IsNullOrWhiteSpace(checkType)) return true;
            var t = checkType.Trim();
            if (NeverShownChecks.Contains(t)) return true;
            var lower = t.ToLowerInvariant();
            return NeverShownWords.Any(w => lower.Contains(w));
        }

        private static MyQualityTileVM Tile(int count, int total, int prevCount, int prevTotal, bool higherIsBetter)
        {
            var pct = Percent(count, total);
            var prev = Percent(prevCount, prevTotal);
            string trend = null;
            if (pct.HasValue && prev.HasValue)
                trend = pct > prev ? (higherIsBetter ? "Up" : "Down") : pct < prev ? (higherIsBetter ? "Down" : "Up") : "Same";
            return new MyQualityTileVM { Count = count, Total = total, Percent = pct, PreviousPercent = prev, Trend = trend };
        }

        private static int? Percent(int part, int total) =>
            total > 0 ? (int?)(int)Math.Round(part * 100.0 / total, MidpointRounding.AwayFromZero) : null;

        private static int SeverityWeight(string severity)
        {
            var s = NormalizeSeverity(severity);
            return s == null ? 1 : 6 - (s[1] - '0');
        }

        private static string NormalizeSeverity(string severity)
        {
            var s = severity?.Trim().ToUpperInvariant();
            return s != null && s.Length == 2 && s[0] == 'P' && s[1] >= '0' && s[1] <= '5' ? s : null;
        }

        private static MyQualityEvidenceVM Evidence(int interviewId, int? irfId, string irfName, DateTime? dateIst) =>
            new MyQualityEvidenceVM { InterviewId = interviewId, IrfId = irfId, IrfName = irfName, DateIst = Day(dateIst) };

        private static string Day(DateTime? value) =>
            value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
