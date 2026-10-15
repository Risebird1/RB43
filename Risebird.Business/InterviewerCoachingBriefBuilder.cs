using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;

namespace Risebird.Business
{
    /// <summary>
    /// Turns raw exposure evidence (risk checks, Payment SLA reasons, trial factors, channel standing)
    /// into a coaching brief grouped by the behaviour to change, and renders the summary email.
    /// Pure: no I/O, so it is unit-tested directly.
    /// </summary>
    public static class InterviewerCoachingBriefBuilder
    {
        public const string SourceRiskCheck = "Risk check";
        public const string SourceQcReview = "QC report review";
        public const string SourceAiReview = "AI review";
        public const string SourceTrial = "Trial";
        public const string SourceAudit = "IRF audit";
        public const string SourceRedMarkers = "Red markers";
        public const string SourceTotalInterviews = "Total Interviews";

        public const string ThemeJoinOnTime = "JoinOnTime";
        public const string ThemeCameraProctoring = "CameraProctoring";
        public const string ThemeRedo = "Redo";
        public const string ThemeReportQuality = "ReportQuality";
        public const string ThemeRubricDecision = "RubricDecision";
        public const string ThemeQuestions = "Questions";
        public const string ThemeInterviewTime = "InterviewTime";
        public const string ThemeCandidateExperience = "CandidateExperience";
        public const string ThemeSetup = "Setup";
        public const string ThemeConduct = "Conduct";
        public const string ThemeOther = "Other";
        public const string ThemeChannelStanding = "ChannelStanding";

        private sealed class ThemeDef
        {
            public ThemeDef(string key, string title, string say, bool isInternal = false)
            {
                Key = key; Title = title; Say = say; IsInternal = isInternal;
            }
            public string Key { get; }
            public string Title { get; }
            public string Say { get; }
            public bool IsInternal { get; }
        }

        private static readonly IReadOnlyList<ThemeDef> Catalogue = new[]
        {
            new ThemeDef(ThemeJoinOnTime, "Join every interview on time",
                "Join 5 minutes before the start. Late joins are flagged automatically and count against you on every channel."),
            new ThemeDef(ThemeCameraProctoring, "Keep your camera on and act on proctoring",
                "Keep your camera on for the whole interview and act on proctoring alerts. Interviews with the camera off cannot be used."),
            new ThemeDef(ThemeRedo, "Run interviews that do not need a redo",
                "A redo costs the candidate and the client a second slot. Check your setup and cover every skill so the interview stands on its own."),
            new ThemeDef(ThemeReportQuality, "Write reports that match what happened",
                "Comments must describe what the candidate actually said and did. Reports are checked against the interview transcript."),
            new ThemeDef(ThemeRubricDecision, "Score to the rubric and match your decision to it",
                "Score each skill against the rubric first, then decide. Your scores and your select or reject decision must agree."),
            new ThemeDef(ThemeQuestions, "Ask enough relevant, in-depth questions",
                "Ask at least five questions on the skills in the job description, and go deeper with follow-ups and hands-on tasks."),
            new ThemeDef(ThemeInterviewTime, "Use the full interview time",
                "Short interviews do not give the client enough signal. Plan your time so every skill gets covered."),
            new ThemeDef(ThemeCandidateExperience, "Give candidates a good experience",
                "Introduce the format, stay clear and professional, and keep the candidate at ease. Low candidate ratings reach the client."),
            new ThemeDef(ThemeSetup, "Have a reliable setup",
                "Use a stable connection, a quiet room and a working headset, and test them before the interview starts."),
            new ThemeDef(ThemeConduct, "Keep every interview fair and professional",
                "Stick to job-related questions, stay neutral, and never help or coach the candidate."),
            new ThemeDef(ThemeOther, "Other review findings",
                "Go through these with the interviewer and agree what to do differently."),
            new ThemeDef(ThemeChannelStanding, "Channel standing",
                "Internal context: audit decisions, red markers and Total Interviews status. Use it to decide, do not read it out.", isInternal: true)
        };

        private static readonly IReadOnlyDictionary<string, ThemeDef> ThemesByKey =
            Catalogue.ToDictionary(t => t.Key, StringComparer.OrdinalIgnoreCase);

        /// <summary>RiskCategory check type → (theme, plain words). Matches usp_AddUpdateRiskCategory.</summary>
        private static readonly IReadOnlyDictionary<string, (string Theme, string Text)> RiskChecks =
            new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
            {
                ["InterviewerLateJoin"] = (ThemeJoinOnTime, "Joined the interview late"),
                ["Interviewer No Show"] = (ThemeJoinOnTime, "Did not join the interview"),
                ["No Show"] = (ThemeJoinOnTime, "Interview did not happen: nobody joined"),
                ["InterviewerVideoOff"] = (ThemeCameraProctoring, "Camera was off during the interview"),
                ["AIRedo"] = (ThemeRedo, "AI review asked for the interview to be redone"),
                ["CandidateRedoRequest"] = (ThemeRedo, "Candidate asked for a redo"),
                ["Report AI Score"] = (ThemeReportQuality, "Low report quality score"),
                ["LessSelectComment"] = (ThemeReportQuality, "Select comment was too short"),
                ["LessRejectComment"] = (ThemeReportQuality, "Reject comment was too short"),
                ["Selectdeficit"] = (ThemeRubricDecision, "Selected despite low skill scores"),
                ["Rejectdeficit"] = (ThemeRubricDecision, "Rejected despite high skill scores"),
                ["Contradiction AI Score"] = (ThemeRubricDecision, "Decision contradicts the interview evidence"),
                ["AIVerdictLessthan4"] = (ThemeRubricDecision, "AI review rated the evaluation low"),
                ["Transcript AI Score"] = (ThemeQuestions, "Low transcript quality score"),
                ["LessQuestions"] = (ThemeQuestions, "Asked fewer than 5 questions"),
                ["deficitmorethan10"] = (ThemeInterviewTime, "Interview ran more than 10 minutes short"),
                ["CandidateRating"] = (ThemeCandidateExperience, "Low candidate rating"),
            };

        /// <summary>Checks that are not the interviewer's doing and never go in a brief.</summary>
        private static readonly HashSet<string> IgnoredRiskChecks =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Candidate No Show" };

        /// <summary>Checklist option text saved on compliant rows (see usp_AddUpdateRiskCategory); not an issue.</summary>
        private static readonly HashSet<string> CompliantReasonTokens =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Video Off When Mandatory" };

        private static readonly (string Theme, string Label, Func<ExposurePaymentRowVM, string> Value)[] PaymentReasons =
        {
            (ThemeRubricDecision, "Decision accuracy", p => p.Inaccuratedecisionmaking),
            (ThemeRubricDecision, "Scoring rubric", p => p.ScoreRubricIgnorance),
            (ThemeRubricDecision, "Status update", p => p.WrongStatusUpdates),
            (ThemeInterviewTime, "Time management", p => p.TimeManagement),
            (ThemeQuestions, "Question relevance", p => p.QuestionsRelevance),
            (ThemeQuestions, "Hands-on coding", p => p.LimitedHandoncoding),
            (ThemeReportQuality, "Feedback comments", p => p.PoorFeedbackComments),
            (ThemeCandidateExperience, "Candidate interaction", p => p.CandidateInteractionQuality),
            (ThemeCandidateExperience, "Communication", p => p.PoorCommunicationSkills),
            (ThemeSetup, "Infrastructure", p => p.WeakInfrastructure),
            (ThemeCameraProctoring, "Proctoring", p => p.ProctoringIgnorance),
            (ThemeCameraProctoring, "Interviewer video", p => p.InterviewerVideo),
            (ThemeConduct, "Conduct", p => p.UnethicalPractices),
            (ThemeJoinOnTime, "Joining on time", p => p.AttendanceJoinCompliance),
        };

        public static IReadOnlyList<string> ThemeKeys => Catalogue.Select(t => t.Key).ToList();

        public static bool IsInternalTheme(string key) =>
            key != null && ThemesByKey.TryGetValue(key, out var t) && t.IsInternal;

        public static string ThemeTitle(string key) =>
            key != null && ThemesByKey.TryGetValue(key, out var t) ? t.Title : key;

        /// <summary>
        /// Theme for a RiskCategory check code. False for unknown codes and for checks that are not the
        /// interviewer's doing (Candidate No Show). Used by the interviewer's My Quality page.
        /// </summary>
        public static bool TryGetRiskCheckTheme(string checkType, out string theme)
        {
            theme = null;
            if (string.IsNullOrWhiteSpace(checkType)) return false;
            var key = checkType.Trim();
            if (IgnoredRiskChecks.Contains(key)) return false;
            if (!RiskChecks.TryGetValue(key, out var map)) return false;
            theme = map.Theme;
            return true;
        }

        /// <summary>Distinct themes behind the reason columns of one Payment SLA row (same map as the coaching brief).</summary>
        public static IReadOnlyList<string> PaymentReasonThemes(ExposurePaymentRowVM payment)
        {
            if (payment == null) return Array.Empty<string>();
            return PaymentReasons
                .Where(r => ReasonParts(r.Value(payment)) != null)
                .Select(r => r.Theme)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Translates a RiskCategory check code into plain words (unknown codes are returned as-is).</summary>
        public static string DescribeRiskCheck(string checkType) =>
            checkType != null && RiskChecks.TryGetValue(checkType.Trim(), out var m) ? m.Text : checkType?.Trim();

        /// <summary>"CheckA:P0,CheckB:P2" (or "CheckA,CheckB") → (type, severity) pairs; severity may be null.</summary>
        public static IEnumerable<(string Type, string Severity)> ParseChecks(string severityBreakdown, string description)
        {
            var source = !string.IsNullOrWhiteSpace(severityBreakdown) ? severityBreakdown : description;
            if (string.IsNullOrWhiteSpace(source))
                yield break;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in source.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var token = raw.Trim();
                if (token.Length == 0) continue;
                string type = token, severity = null;
                var colon = token.LastIndexOf(':');
                if (colon > 0)
                {
                    type = token.Substring(0, colon).Trim();
                    var sev = token.Substring(colon + 1).Trim().ToUpperInvariant();
                    severity = sev.Length == 2 && sev[0] == 'P' && char.IsDigit(sev[1]) ? sev : null;
                }
                if (type.Length > 0 && seen.Add(type))
                    yield return (type, severity);
            }
        }

        /// <summary>
        /// Meaningful parts of a Payment SLA reason value: splits ticked options, drops blanks / No / N/A and
        /// compliant checklist text. Returns null when there is no issue; an empty list for a bare "Yes".
        /// </summary>
        public static IReadOnlyList<string> ReasonParts(string value)
        {
            if (!TrialFlagEvidenceBadgeHelper.IsPositiveEvidence(value))
                return null;
            var parts = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => TrialFlagEvidenceBadgeHelper.IsPositiveEvidence(p) && !CompliantReasonTokens.Contains(p))
                .ToList();
            if (parts.Count == 0)
                return null;
            var bareYes = parts.All(p => p.Equals("yes", StringComparison.OrdinalIgnoreCase)
                                      || p.Equals("y", StringComparison.OrdinalIgnoreCase)
                                      || p.Equals("1", StringComparison.OrdinalIgnoreCase)
                                      || p.Equals("true", StringComparison.OrdinalIgnoreCase));
            return bareYes ? Array.Empty<string>() : parts;
        }

        public static CoachingBriefVM Build(InterviewerExposureVM exposure, DateTime fromIst, DateTime toIst, DateTime nowUtc)
        {
            var profile = exposure?.Profile;
            var items = new List<(string Theme, CoachingEvidenceVM Item)>();

            foreach (var r in exposure?.Risks ?? Array.Empty<ExposureRiskRowVM>())
            {
                foreach (var (type, severity) in ParseChecks(r.SeverityBreakdown, r.Description))
                {
                    if (IgnoredRiskChecks.Contains(type)) continue;
                    var known = RiskChecks.TryGetValue(type, out var map);
                    items.Add((known ? map.Theme : ThemeOther, new CoachingEvidenceVM
                    {
                        Text = known ? map.Text : type,
                        Source = SourceRiskCheck,
                        Severity = severity,
                        IrfName = r.IrfName,
                        DateIst = Day(r.InterviewDateIst),
                        InterviewId = r.InterviewId
                    }));
                }
            }

            foreach (var p in exposure?.Payments ?? Array.Empty<ExposurePaymentRowVM>())
            {
                var deduction = p.PaymentDeduction.GetValueOrDefault();
                var savedBy = p.LastSavedBy?.Trim();
                var paymentSource = string.Equals(savedBy, "AISystem", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(savedBy, "System", StringComparison.OrdinalIgnoreCase)
                    ? SourceAiReview
                    : SourceQcReview;
                var suffix = deduction > 0 ? $" (payment deduction {deduction}%)" : "";
                var any = false;
                foreach (var (theme, label, value) in PaymentReasons)
                {
                    var parts = ReasonParts(value(p));
                    if (parts == null) continue;
                    any = true;
                    items.Add((theme, new CoachingEvidenceVM
                    {
                        Text = (parts.Count == 0 ? label : label + ": " + string.Join(", ", parts)) + suffix,
                        Source = paymentSource,
                        IrfName = p.IrfName,
                        DateIst = Day(p.InterviewDateIst),
                        InterviewId = p.InterviewId
                    }));
                }
                if (!any && deduction > 0)
                {
                    items.Add((ThemeOther, new CoachingEvidenceVM
                    {
                        Text = (paymentSource == SourceAiReview ? "Report penalised by the AI review" : "Report penalised in QC review") + suffix,
                        Source = paymentSource,
                        IrfName = p.IrfName,
                        DateIst = Day(p.InterviewDateIst),
                        InterviewId = p.InterviewId
                    }));
                }
            }

            foreach (var t in exposure?.Trials ?? Array.Empty<ExposureTrialRowVM>())
            {
                const string onTrial = " on your first interview for this channel";
                void Add(string theme, string text) => items.Add((theme, new CoachingEvidenceVM
                {
                    Text = text + onTrial,
                    Source = SourceTrial,
                    IrfName = t.IrfName,
                    DateIst = Day(t.TriggeringInterviewDate),
                    InterviewId = t.TriggeringInterviewId
                }));

                if (t.JoinTimePass == false)
                    Add(ThemeJoinOnTime, t.JoinTimeMinutes > 0 ? $"Joined late by {Num(t.JoinTimeMinutes)} min" : "Joined late");
                if (t.InterviewDurationPass == false)
                    Add(ThemeInterviewTime, t.InterviewDurationMinutes > 0 ? $"Short interview ({Num(t.InterviewDurationMinutes)} min)" : "Short interview");
                if (t.TranscriptQualityPass == false)
                    Add(ThemeQuestions, t.TranscriptQualityScore.HasValue ? $"Low transcript quality ({Num(t.TranscriptQualityScore)}/10)" : "Low transcript quality");
                if (t.ReportQualityPass == false)
                    Add(ThemeReportQuality, "Report quality below the bar");
                if (t.DecisionQualityPass == false)
                    Add(ThemeRubricDecision, "Decision did not match the evidence");
                if (t.CandidateRatingPass == false)
                    Add(ThemeCandidateExperience, t.CandidateRating.HasValue ? $"Low candidate rating ({Num(t.CandidateRating)}/5)" : "Low candidate rating");

                var status = t.ReviewStatus?.Trim().ToLowerInvariant();
                if (status == TrialFlagConstants.ReviewPending || status == TrialFlagConstants.ReviewRemoved)
                {
                    items.Add((ThemeChannelStanding, new CoachingEvidenceVM
                    {
                        Text = status == TrialFlagConstants.ReviewPending
                            ? "Trial flag awaiting a decision"
                            : "Removed after trial review" + (string.IsNullOrWhiteSpace(t.OverallVerdict) ? "" : $" ({t.OverallVerdict})"),
                        Source = SourceTrial,
                        IrfName = t.IrfName,
                        DateIst = Day(t.TriggeringInterviewDate),
                        InterviewId = t.TriggeringInterviewId,
                        IsOpen = status == TrialFlagConstants.ReviewPending
                    }));
                }
            }

            foreach (var s in exposure?.Standing ?? Array.Empty<ExposureStandingRowVM>())
            {
                var source = string.Equals(s.Kind, "Audit", StringComparison.OrdinalIgnoreCase) ? SourceAudit
                    : string.Equals(s.Kind, "RedMarker", StringComparison.OrdinalIgnoreCase) ? SourceRedMarkers
                    : SourceTotalInterviews;
                items.Add((ThemeChannelStanding, new CoachingEvidenceVM
                {
                    Text = s.Detail,
                    Source = source,
                    IrfName = s.IrfName,
                    DateIst = Day(s.AtIst),
                    InterviewId = s.InterviewId,
                    IsOpen = s.IsOpen
                }));
            }

            var themes = items
                .GroupBy(x => x.Theme)
                .Select(g =>
                {
                    var def = ThemesByKey[g.Key];
                    var list = g.Select(x => x.Item)
                        .OrderByDescending(i => i.IsOpen)
                        .ThenBy(i => SeverityRank(i.Severity))
                        .ThenByDescending(i => i.DateIst)
                        .ToList();
                    return new CoachingThemeVM
                    {
                        Key = def.Key,
                        Title = def.Title,
                        Say = def.Say,
                        IsInternal = def.IsInternal,
                        WorstSeverity = list.Select(i => i.Severity).Where(v => v != null).OrderBy(SeverityRank).FirstOrDefault(),
                        Sources = list.Select(i => i.Source).Distinct().ToList(),
                        Items = list
                    };
                })
                .OrderBy(t => t.IsInternal)
                .ThenBy(t => SeverityRank(t.WorstSeverity))
                .ThenByDescending(t => t.OpenCount)
                .ThenByDescending(t => t.ItemCount)
                .ThenBy(t => ThemeKeys.ToList().IndexOf(t.Key))
                .ToList();

            var lastEmail = profile?.LastSummaryEmailAtUtc;
            var emailAvailableAt = lastEmail?.AddHours(CoachingConstants.EmailCooldownHours);

            return new CoachingBriefVM
            {
                InterviewerEmail = profile?.InterviewerEmail,
                InterviewerName = profile?.InterviewerName ?? profile?.InterviewerEmail,
                FirstName = profile?.FirstName ?? profile?.InterviewerName,
                Mobile = profile?.Mobile,
                IsActive = profile?.IsActive ?? false,
                FromIst = fromIst.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ToIst = toIst.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Themes = themes,
                IssueCount = themes.Sum(t => t.ItemCount),
                AreaCount = themes.Count,
                OpenCount = themes.Sum(t => t.OpenCount),
                CoachingSessionsInRange = profile?.CoachingSessionsInRange ?? 0,
                LastCoachedAtIst = profile?.LastCoachedAtIst,
                LastCoachingOutcome = profile?.LastCoachingOutcome,
                LastCoachEmail = profile?.LastCoachEmail,
                // Inactive accounts are never emailed (the rule NotificationHelper.SendMail documents).
                CanEmail = profile != null && profile.IsActive && (emailAvailableAt == null || emailAvailableAt <= nowUtc),
                EmailAvailableAtUtc = emailAvailableAt > nowUtc ? emailAvailableAt : null
            };
        }

        /// <summary>
        /// Summary email for the interviewer. Only the selected, non-internal themes go out; severity codes,
        /// reviewer names and channel standing never do. Every value is HTML-encoded.
        /// </summary>
        public static (string Subject, string Html, int ThemeCount) BuildEmail(CoachingBriefVM brief, IEnumerable<string> selectedThemeKeys, string note)
        {
            var selected = new HashSet<string>(selectedThemeKeys ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var themes = (brief?.Themes ?? Array.Empty<CoachingThemeVM>())
                .Where(t => !t.IsInternal && t.ItemCount > 0 && selected.Contains(t.Key))
                .ToList();

            var count = themes.Count;
            var subject = $"Your Risebird interview review: {count} area{(count == 1 ? "" : "s")} to work on";

            string E(string s) => WebUtility.HtmlEncode(s ?? "");
            var greetingName = !string.IsNullOrWhiteSpace(brief?.InterviewerName) ? brief.InterviewerName : "there";

            var html = new StringBuilder();
            html.Append("<div style=\"font-family:Arial,Helvetica,sans-serif;font-size:14px;line-height:1.55;color:#0f172a;max-width:640px\">");
            html.Append("<p>Hi ").Append(E(greetingName)).Append(",</p>");

            if (!string.IsNullOrWhiteSpace(note))
            {
                foreach (var para in note.Trim().Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries))
                    html.Append("<p>").Append(E(para.Trim()).Replace("\r\n", "<br/>").Replace("\n", "<br/>")).Append("</p>");
            }

            html.Append("<p>We reviewed your interviews from ").Append(E(LongDay(brief?.FromIst)))
                .Append(" to ").Append(E(LongDay(brief?.ToIst)))
                .Append(". Here is everything in one place, so you can work on it together rather than piece by piece.</p>");

            foreach (var t in themes)
            {
                html.Append("<h3 style=\"font-size:16px;margin:20px 0 6px\">").Append(E(t.Title)).Append("</h3>");
                html.Append("<p style=\"margin:0 0 6px\">").Append(E(t.Say)).Append("</p>");
                html.Append("<ul style=\"margin:0 0 10px;padding-left:20px\">");
                foreach (var i in t.Items)
                {
                    html.Append("<li>").Append(E(i.Text));
                    var context = string.Join(", ", new[] { i.IrfName, LongDay(i.DateIst) }.Where(x => !string.IsNullOrWhiteSpace(x)));
                    if (context.Length > 0)
                        html.Append(" <span style=\"color:#64748b\">(").Append(E(context)).Append(")</span>");
                    html.Append("</li>");
                }
                html.Append("</ul>");
            }

            html.Append("<p>If anything here looks wrong, or you want to talk it through, just reply to this email.</p>");
            html.Append("<p>Thanks,<br/>").Append(E(CoachingConstants.FromName)).Append("</p>");
            html.Append("</div>");

            return (subject, html.ToString(), count);
        }

        private static int SeverityRank(string severity) =>
            severity != null && severity.Length == 2 && severity[0] == 'P' && char.IsDigit(severity[1]) ? severity[1] - '0' : 9;

        private static string Day(DateTime? value) =>
            value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        private static string LongDay(string ymd) =>
            DateTime.TryParseExact(ymd, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                ? d.ToString("d MMM yyyy", CultureInfo.InvariantCulture)
                : ymd;

        private static string Num(decimal? value) =>
            value?.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
