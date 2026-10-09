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
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                result.Error = LoadErrorMessage;
                result.ErrorDetail = ex.GetBaseException().Message;
            }
            return result;
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

        private static string NormalizeEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;
            var trimmed = email.Trim().Replace("\"", "");
            return trimmed.Length == 0 ? null : trimmed;
        }
    }
}
