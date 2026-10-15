using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class QCActivityDashboardService : IQCActivityDashboardService
    {
        private const string DateFormat = "yyyy-MM-dd";
        private const string LoadErrorMessage = "Could not load QC activity. Please try again or narrow the date range.";

        private readonly IQCActivityDashboardRepository _repository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly Func<DateTime> _todayIst;

        public QCActivityDashboardService(IQCActivityDashboardRepository repository, IApplicationErrorLogger applicationErrorLogger)
            : this(repository, applicationErrorLogger, () => DateTime.UtcNow.AddMinutes(330).Date)
        {
        }

        /// <summary>Test seam: lets tests pin "today" in IST. Internal so DI only sees the public constructor.</summary>
        internal QCActivityDashboardService(IQCActivityDashboardRepository repository, IApplicationErrorLogger applicationErrorLogger,
            Func<DateTime> todayIst)
        {
            _repository = repository;
            _applicationErrorLogger = applicationErrorLogger;
            _todayIst = todayIst;
        }

        /// <summary>
        /// Inclusive IST calendar range: defaults to the last 7 days ending today, never ends after today,
        /// swaps a reversed range, and keeps at most 92 days (the start moves forward).
        /// </summary>
        public static (DateTime From, DateTime To, bool Capped) NormalizeRange(DateTime? fromIst, DateTime? toIst, DateTime todayIst)
        {
            var today = todayIst.Date;
            var to = (toIst ?? today).Date;
            if (to > today)
                to = today;

            var from = (fromIst ?? to.AddDays(-(QCActivityMetrics.DefaultRangeDays - 1))).Date;
            if (from > to)
                (from, to) = (to, from);

            var capped = false;
            if ((to - from).TotalDays + 1 > QCActivityMetrics.MaxRangeDays)
            {
                from = to.AddDays(-(QCActivityMetrics.MaxRangeDays - 1));
                capped = true;
            }
            return (from, to, capped);
        }

        public async Task<QCActivityDashboardDataVM> GetSummaryAsync(DateTime? fromIst, DateTime? toIst, bool onlyQualityController,
            string restrictToActorEmail = null)
        {
            var (from, to, capped) = NormalizeRange(fromIst, toIst, _todayIst());
            var restrict = NormalizeEmail(restrictToActorEmail);
            var result = new QCActivityDashboardDataVM
            {
                FromIst = from.ToString(DateFormat),
                ToIst = to.ToString(DateFormat),
                OnlyQualityController = onlyQualityController,
                RangeWasCapped = capped
            };

            try
            {
                var (rows, backlog) = await _repository.GetSummaryAsync(from, to, onlyQualityController, restrict);
                IReadOnlyList<QCActivitySummaryRowVM> list = rows ?? Array.Empty<QCActivitySummaryRowVM>();

                // Defence in depth: a restricted viewer only ever gets their own row.
                if (restrict != null)
                {
                    list = list.Where(r => string.Equals(r.ActorEmail?.Trim(), restrict, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                result.Rows = list;
                result.Backlog = backlog ?? new QCActivityBacklogVM();
                result.Totals = QCActivityTotalsVM.From(list.ToList());
                // Page-level high-risk coverage (summary result sets 3 and 4).
                result.Coverage = result.Backlog.Coverage ?? new QCActivityCoverageVM();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                result.Error = LoadErrorMessage;
                result.ErrorDetail = ex.GetBaseException().Message;
            }
            return result;
        }

        public async Task<QCActivityDetailsResultVM> GetDetailsAsync(string actorEmail, string metric, DateTime? fromIst, DateTime? toIst,
            string restrictToActorEmail = null)
        {
            var (from, to, _) = NormalizeRange(fromIst, toIst, _todayIst());
            var actor = NormalizeEmail(restrictToActorEmail) ?? NormalizeEmail(actorEmail);
            var key = QCActivityMetrics.Normalize(metric);
            var result = new QCActivityDetailsResultVM
            {
                ActorEmail = actor,
                Metric = key ?? metric,
                MetricLabel = QCActivityMetrics.Label(metric),
                FromIst = from.ToString(DateFormat),
                ToIst = to.ToString(DateFormat)
            };

            if (key == null || actor == null)
                return result;

            try
            {
                var rows = await _repository.GetDetailsAsync(actor, key, from, to) ?? Array.Empty<QCActivityDetailRowVM>();
                result.Rows = rows;
                result.Truncated = rows.Count >= QCActivityMetrics.MaxDetailRows;

                // Part 2: say why a scored action is not authentic, with the thresholds in force.
                if (rows.Any(r => r.IsAuthentic == false && r.Metric != QCActivityMetrics.PlivoCall))
                {
                    var settings = await LoadSettingsAsync();
                    foreach (var r in rows.Where(r => r.IsAuthentic == false && r.Metric != QCActivityMetrics.PlivoCall))
                        r.NotAuthenticReason = settings.NotAuthenticReason(r.Comment, r.HasEvidence == true);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                result.Error = LoadErrorMessage;
                result.ErrorDetail = ex.GetBaseException().Message;
            }
            return result;
        }

        /// <summary>
        /// Flags panel: one row per flag per person with the rows behind it and the rule (thresholds read from
        /// QCActivitySetting). <paramref name="actorEmail"/> null = everyone (managers); a restricted viewer only ever
        /// gets their own flags.
        /// </summary>
        public async Task<QCActivityFlagsResultVM> GetFlagsAsync(string actorEmail, DateTime? fromIst, DateTime? toIst,
            string restrictToActorEmail = null)
        {
            var (from, to, _) = NormalizeRange(fromIst, toIst, _todayIst());
            var restrict = NormalizeEmail(restrictToActorEmail);
            var actor = restrict ?? NormalizeEmail(actorEmail);
            var result = new QCActivityFlagsResultVM
            {
                ActorEmail = actor,
                FromIst = from.ToString(DateFormat),
                ToIst = to.ToString(DateFormat)
            };

            try
            {
                var (flags, items, settingRows) = await _repository.GetFlagsAsync(from, to, actor);
                result.Settings = QCActivitySettingsVM.From(settingRows);
                result.Flags = BuildFlags(flags, items, result.Settings, restrict);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                result.Error = LoadErrorMessage;
                result.ErrorDetail = ex.GetBaseException().Message;
            }
            return result;
        }

        /// <summary>High-Risk interviewers in the window and how many any QC person touched (page level).</summary>
        public async Task<QCActivityCoverageVM> GetCoverageAsync(DateTime? fromIst, DateTime? toIst)
        {
            var (from, to, _) = NormalizeRange(fromIst, toIst, _todayIst());
            try
            {
                return await _repository.GetCoverageAsync(from, to) ?? new QCActivityCoverageVM();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new QCActivityCoverageVM { Error = LoadErrorMessage, ErrorDetail = ex.GetBaseException().Message };
            }
        }

        /// <summary>Flag titles and rules with the thresholds in force; items grouped under their flag; restricted to one person when set.</summary>
        public static IReadOnlyList<QCActivityFlagVM> BuildFlags(IEnumerable<QCActivityFlagVM> flags, IEnumerable<QCActivityFlagItemVM> items,
            QCActivitySettingsVM settings, string restrictToActorEmail = null)
        {
            settings ??= new QCActivitySettingsVM();
            var restrict = NormalizeEmail(restrictToActorEmail);
            var itemList = (items ?? Enumerable.Empty<QCActivityFlagItemVM>()).Where(i => i != null).ToList();
            return (flags ?? Enumerable.Empty<QCActivityFlagVM>())
                .Where(f => f != null && (restrict == null || string.Equals(f.ActorEmail?.Trim(), restrict, StringComparison.OrdinalIgnoreCase)))
                .Select(f =>
                {
                    var (title, rule) = DescribeFlag(f.Flag, settings);
                    f.Title = title;
                    f.Rule = rule;
                    f.Items = itemList
                        .Where(i => string.Equals(i.Flag, f.Flag, StringComparison.OrdinalIgnoreCase)
                                    && string.Equals(i.ActorEmail?.Trim(), f.ActorEmail?.Trim(), StringComparison.OrdinalIgnoreCase)
                                    && (f.Flag != "Burst" || string.IsNullOrEmpty(f.Metrics) || string.Equals(i.Metric, f.Metrics, StringComparison.OrdinalIgnoreCase)))
                        .OrderByDescending(i => i.ActionAtIst)
                        .ToList();
                    return f;
                })
                .ToList();
        }

        /// <summary>Readable title and rule for a flag key, using the thresholds in force.</summary>
        public static (string Title, string Rule) DescribeFlag(string flag, QCActivitySettingsVM s)
        {
            s ??= new QCActivitySettingsVM();
            switch (flag)
            {
                case "Burst":
                    return ("Burst of actions", $"{s.BurstCount} or more actions of the same type within {s.BurstMinutes} minutes");
                case "ReviewedNoCall":
                    return ("Reviewed with no call", "Moved to Reviewed (Total Interviews or Total Red Marker) with no connected call or coaching to that interviewer in the window");
                case "ReversedByOther":
                    return ("Reversed by another QC", $"Reviewed, Blocked, Suspicious or Inactivated undone by a different person within {s.ReversalDays} days");
                case "ShortCalls":
                    return ("Short calls", $"More than {s.ShortCallPct}% of dialled calls lasted under {s.ShortCallSeconds} seconds");
                case "KeepDespiteP0":
                    return ("Kept despite a P0", "IRF audit or trial Keep on an interviewer with a P0 graded interview in the same window");
                case "NoEvidence":
                    return ("Actions without evidence", "No graded issue, AI penalty, open trial or audit flag, or red marker on the interviewer in the window");
                default:
                    return (flag, null);
            }
        }

        private async Task<QCActivitySettingsVM> LoadSettingsAsync()
        {
            try
            {
                return QCActivitySettingsVM.From(await _repository.GetSettingsAsync());
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new QCActivitySettingsVM();
            }
        }

        /// <summary>Minimum completed interviews for the Interviewer QC View: default 3, clamped to 1..50.</summary>
        public static int ClampMinInterviews(int? minInterviews)
        {
            var value = minInterviews ?? InterviewerQCViewLimits.DefaultMinInterviews;
            if (value < InterviewerQCViewLimits.MinMinInterviews)
                return InterviewerQCViewLimits.MinMinInterviews;
            if (value > InterviewerQCViewLimits.MaxMinInterviews)
                return InterviewerQCViewLimits.MaxMinInterviews;
            return value;
        }

        public async Task<InterviewerQCListResultVM> GetInterviewerViewAsync(DateTime? fromIst, DateTime? toIst, string search = null,
            int? minInterviews = null, bool includeOpenFlags = false)
        {
            var (from, to, capped) = NormalizeRange(fromIst, toIst, _todayIst());
            var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            if (term != null && term.Length > 150)
                term = term.Substring(0, 150);
            var min = ClampMinInterviews(minInterviews);

            var result = new InterviewerQCListResultVM
            {
                FromIst = from.ToString(DateFormat),
                ToIst = to.ToString(DateFormat),
                Search = term,
                MinInterviews = min,
                IncludeOpenFlags = includeOpenFlags,
                RangeWasCapped = capped
            };

            try
            {
                var rows = await _repository.GetInterviewerViewAsync(from, to, term, null, min, includeOpenFlags) ?? Array.Empty<InterviewerQCRowVM>();
                result.Rows = rows;
                result.Totals = InterviewerQCTotalsVM.From(rows.ToList());
                result.Truncated = rows.Count >= InterviewerQCViewLimits.MaxInterviewers;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                result.Error = LoadErrorMessage;
                result.ErrorDetail = ex.GetBaseException().Message;
            }
            return result;
        }

        public async Task<InterviewerQCTimelineVM> GetInterviewerTimelineAsync(string interviewerEmail, DateTime? fromIst, DateTime? toIst)
        {
            var (from, to, _) = NormalizeRange(fromIst, toIst, _todayIst());
            var email = NormalizeEmail(interviewerEmail);
            var result = new InterviewerQCTimelineVM
            {
                InterviewerEmail = email,
                FromIst = from.ToString(DateFormat),
                ToIst = to.ToString(DateFormat)
            };
            if (email == null)
                return result;

            try
            {
                var summary = await _repository.GetInterviewerViewAsync(from, to, null, email);
                result.Summary = summary?.FirstOrDefault();
                if (result.Summary == null)
                {
                    result.Error = "No interviewer found with that email.";
                    return result;
                }

                var (irfs, events) = await _repository.GetInterviewerTimelineAsync(email, from, to);
                result.Irfs = irfs ?? Array.Empty<InterviewerQCIrfVM>();
                result.Events = events ?? Array.Empty<InterviewerQCEventVM>();
                result.Truncated = result.Events.Count >= InterviewerQCViewLimits.MaxTimelineEvents;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                result.Error = LoadErrorMessage;
                result.ErrorDetail = ex.GetBaseException().Message;
            }
            return result;
        }

        public async Task<QCActivityDiagnosticsResultVM> GetDiagnosticsAsync(DateTime? fromIst, DateTime? toIst)
        {
            var (from, to, capped) = NormalizeRange(fromIst, toIst, _todayIst());
            var result = new QCActivityDiagnosticsResultVM
            {
                FromIst = from.ToString(DateFormat),
                ToIst = to.ToString(DateFormat),
                RangeWasCapped = capped
            };

            try
            {
                result.Rows = await _repository.GetDiagnosticsAsync(from, to) ?? Array.Empty<QCActivityDiagnosticRowVM>();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                result.Error = LoadErrorMessage;
                result.ErrorDetail = ex.GetBaseException().Message;
            }
            return result;
        }

        public async Task<ReportEditVM> GetReportEditAsync(int id)
        {
            if (id <= 0)
                return null;

            try
            {
                return await _repository.GetReportEditAsync(id);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        private static string NormalizeEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;
            var trimmed = email.Trim().Replace("\"", "");
            return trimmed.Length == 0 ? null : trimmed;
        }
    }
}
