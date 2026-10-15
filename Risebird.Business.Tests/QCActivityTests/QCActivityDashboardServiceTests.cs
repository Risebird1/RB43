using Moq;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.QCActivityTests
{
    public class QCActivityDashboardServiceTests
    {
        private static readonly DateTime TodayIst = new DateTime(2026, 10, 1);

        private readonly Mock<IQCActivityDashboardRepository> _repository = new();
        private readonly Mock<IApplicationErrorLogger> _logger = new();

        private QCActivityDashboardService CreateService() =>
            new QCActivityDashboardService(_repository.Object, _logger.Object, () => TodayIst);

        private static QCActivitySummaryRowVM Row(string email, int reports = 0, int actions = 0) =>
            new QCActivitySummaryRowVM { ActorEmail = email, ActorName = email, ReportsReviewed = reports, TotalActions = actions };

        [Fact]
        public void Range_defaults_to_the_last_seven_days_ending_today()
        {
            var (from, to, capped) = QCActivityDashboardService.NormalizeRange(null, null, TodayIst);

            Assert.Equal(new DateTime(2026, 9, 25), from);
            Assert.Equal(TodayIst, to);
            Assert.False(capped);
        }

        [Fact]
        public void Range_longer_than_92_days_keeps_the_last_92()
        {
            var (from, to, capped) = QCActivityDashboardService.NormalizeRange(new DateTime(2026, 1, 1), new DateTime(2026, 9, 30), TodayIst);

            Assert.Equal(new DateTime(2026, 9, 30), to);
            Assert.Equal(92, (to - from).TotalDays + 1);
            Assert.True(capped);
        }

        [Fact]
        public void Exactly_92_days_is_not_capped()
        {
            var (from, to, capped) = QCActivityDashboardService.NormalizeRange(new DateTime(2026, 7, 2), new DateTime(2026, 10, 1), TodayIst);

            Assert.Equal(new DateTime(2026, 7, 2), from);
            Assert.Equal(new DateTime(2026, 10, 1), to);
            Assert.False(capped);
        }

        [Fact]
        public void Reversed_range_is_swapped_and_a_future_end_is_pulled_back_to_today()
        {
            var (from, to, _) = QCActivityDashboardService.NormalizeRange(new DateTime(2026, 9, 30), new DateTime(2026, 9, 20), TodayIst);
            Assert.Equal(new DateTime(2026, 9, 20), from);
            Assert.Equal(new DateTime(2026, 9, 30), to);

            var (_, futureTo, _) = QCActivityDashboardService.NormalizeRange(new DateTime(2026, 9, 28), new DateTime(2026, 12, 31), TodayIst);
            Assert.Equal(TodayIst, futureTo);
        }

        [Fact]
        public async Task Summary_passes_the_normalised_range_and_builds_totals()
        {
            _repository
                .Setup(r => r.GetSummaryAsync(new DateTime(2026, 9, 25), TodayIst, true, null))
                .ReturnsAsync((new List<QCActivitySummaryRowVM> { Row("a@risebird.io", 5, 5), Row("b@risebird.io") },
                               new QCActivityBacklogVM { PendingReports = 12 }));

            var result = await CreateService().GetSummaryAsync(null, null, onlyQualityController: true);

            Assert.Null(result.Error);
            Assert.Equal("2026-09-25", result.FromIst);
            Assert.Equal("2026-10-01", result.ToIst);
            Assert.Equal(2, result.Rows.Count);
            Assert.Equal(12, result.Backlog.PendingReports);
            Assert.Equal(5, result.Totals.ReportsReviewed);
            Assert.Equal(1, result.Totals.UsersWithNoActivity);
            Assert.Equal(2, result.Totals.UsersListed);
        }

        [Fact]
        public async Task Restricted_viewer_only_gets_their_own_row_even_if_the_repository_returns_more()
        {
            _repository
                .Setup(r => r.GetSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, "qc@risebird.io"))
                .ReturnsAsync((new List<QCActivitySummaryRowVM> { Row("qc@risebird.io", 3, 3), Row("other@risebird.io", 9, 9) },
                               new QCActivityBacklogVM()));

            var result = await CreateService().GetSummaryAsync(null, null, true, restrictToActorEmail: " qc@risebird.io ");

            var row = Assert.Single(result.Rows);
            Assert.Equal("qc@risebird.io", row.ActorEmail);
            Assert.Equal(3, result.Totals.ReportsReviewed);
        }

        [Fact]
        public async Task Summary_failure_is_logged_and_returned_as_an_error_message()
        {
            _repository
                .Setup(r => r.GetSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var result = await CreateService().GetSummaryAsync(null, null, true);

            Assert.NotNull(result.Error);
            Assert.Empty(result.Rows);
            _logger.Verify(l => l.LogMessage(LogLevelInfo.Exception, It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
        }

        [Theory]
        [InlineData("DropTable")]
        [InlineData("")]
        [InlineData(null)]
        public async Task Unknown_metric_returns_no_rows_without_querying(string metric)
        {
            var result = await CreateService().GetDetailsAsync("qc@risebird.io", metric, null, null);

            Assert.Empty(result.Rows);
            _repository.Verify(r => r.GetDetailsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task Details_for_a_restricted_viewer_ignore_the_requested_actor_and_use_the_canonical_metric_key()
        {
            _repository
                .Setup(r => r.GetDetailsAsync("qc@risebird.io", QCActivityMetrics.PlivoCall, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<QCActivityDetailRowVM> { new QCActivityDetailRowVM { Metric = "PlivoCall", Amount = 120 } });

            var result = await CreateService().GetDetailsAsync("someone.else@risebird.io", "plivocall", null, null,
                restrictToActorEmail: "qc@risebird.io");

            Assert.Equal("qc@risebird.io", result.ActorEmail);
            Assert.Equal(QCActivityMetrics.PlivoCall, result.Metric);
            Assert.Single(result.Rows);
            Assert.False(result.Truncated);
            _repository.Verify(r => r.GetDetailsAsync("someone.else@risebird.io", It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task Details_at_the_row_cap_are_flagged_as_truncated()
        {
            var rows = Enumerable.Range(0, QCActivityMetrics.MaxDetailRows).Select(_ => new QCActivityDetailRowVM()).ToList();
            _repository
                .Setup(r => r.GetDetailsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(rows);

            var result = await CreateService().GetDetailsAsync("qc@risebird.io", "All", null, null);

            Assert.True(result.Truncated);
        }

        [Fact]
        public async Task Interviewer_view_builds_tile_totals_and_coaching_coverage()
        {
            _repository
                .Setup(r => r.GetInterviewerViewAsync(new DateTime(2026, 9, 25), TodayIst, "ayesha", null,
                    InterviewerQCViewLimits.DefaultMinInterviews, false))
                .ReturnsAsync(new List<InterviewerQCRowVM>
                {
                    new InterviewerQCRowVM { InterviewerEmail = "a@x.com", HasIssue = true, NeedsCall = true, Penalised = 2, OpenFlags = 1, BadInterviews = 2 },
                    new InterviewerQCRowVM { InterviewerEmail = "b@x.com", HasIssue = true, Calls = 1, AiPenalised = 1, RemovedOrInactive = true },
                    new InterviewerQCRowVM { InterviewerEmail = "c@x.com", HasIssue = true, CoachingSessions = 1, BadInterviews = 1 },
                    new InterviewerQCRowVM { InterviewerEmail = "d@x.com" }
                });

            var result = await CreateService().GetInterviewerViewAsync(null, null, "  ayesha ");

            Assert.Null(result.Error);
            Assert.Equal("ayesha", result.Search);
            Assert.Equal(InterviewerQCViewLimits.DefaultMinInterviews, result.MinInterviews);
            Assert.False(result.IncludeOpenFlags);
            Assert.Equal(4, result.Totals.Interviewers);
            Assert.Equal(1, result.Totals.NeedsCall);
            Assert.Equal(1, result.Totals.OpenFlags);
            Assert.Equal(2, result.Totals.Penalised);
            Assert.Equal(1, result.Totals.RemovedOrInactive);
            Assert.Equal(2, result.Totals.WithBadInterviews);
            Assert.Equal(3, result.Totals.WithIssue);
            Assert.Equal(2, result.Totals.WithIssueContacted);
            Assert.Equal(67, result.Totals.CoachingCoveragePercent);
        }

        [Theory]
        [InlineData(null, 3)]
        [InlineData(1, 1)]
        [InlineData(10, 10)]
        [InlineData(50, 50)]
        public void Min_interviews_defaults_to_three_and_keeps_values_in_range(int? requested, int expected)
        {
            Assert.Equal(expected, QCActivityDashboardService.ClampMinInterviews(requested));
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-5, 1)]
        [InlineData(51, 50)]
        [InlineData(int.MaxValue, 50)]
        public void Min_interviews_outside_one_to_fifty_is_clamped(int requested, int expected)
        {
            Assert.Equal(expected, QCActivityDashboardService.ClampMinInterviews(requested));
        }

        [Fact]
        public async Task Interviewer_view_passes_the_clamped_minimum_and_the_open_flags_option_to_the_repository()
        {
            _repository
                .Setup(r => r.GetInterviewerViewAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, null, 50, true))
                .ReturnsAsync(new List<InterviewerQCRowVM> { new InterviewerQCRowVM { InterviewerEmail = "a@x.com" } });

            var result = await CreateService().GetInterviewerViewAsync(null, null, null, minInterviews: 500, includeOpenFlags: true);

            Assert.Null(result.Error);
            Assert.Equal(50, result.MinInterviews);
            Assert.True(result.IncludeOpenFlags);
            Assert.Single(result.Rows);
            _repository.Verify(r => r.GetInterviewerViewAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, null, 50, true), Times.Once);
        }

        [Fact]
        public async Task Diagnostics_pass_the_normalised_range_and_return_the_rows()
        {
            _repository
                .Setup(r => r.GetDiagnosticsAsync(new DateTime(2026, 9, 25), TodayIst))
                .ReturnsAsync(new List<QCActivityDiagnosticRowVM>
                {
                    new QCActivityDiagnosticRowVM { Source = "ClientCommentsForReport.CreatedBy", ActorValue = "qc@risebird.io", ActorRows = 12, Status = "Quality Controller" },
                    new QCActivityDiagnosticRowVM { Source = "TrialFlags.ReviewedBy", Status = "No rows" }
                });

            var result = await CreateService().GetDiagnosticsAsync(null, null);

            Assert.Null(result.Error);
            Assert.Equal("2026-09-25", result.FromIst);
            Assert.Equal("2026-10-01", result.ToIst);
            Assert.Equal(2, result.Rows.Count);
        }

        [Fact]
        public async Task Diagnostics_failure_is_logged_and_returned_as_an_error_message()
        {
            _repository
                .Setup(r => r.GetDiagnosticsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var result = await CreateService().GetDiagnosticsAsync(null, null);

            Assert.NotNull(result.Error);
            Assert.Empty(result.Rows);
            _logger.Verify(l => l.LogMessage(LogLevelInfo.Exception, It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void Coaching_coverage_is_empty_when_nobody_has_an_issue()
        {
            Assert.Null(InterviewerQCTotalsVM.From(new[] { new InterviewerQCRowVM() }).CoachingCoveragePercent);
        }

        [Fact]
        public async Task Timeline_for_an_unknown_interviewer_returns_an_error_without_loading_events()
        {
            _repository
                .Setup(r => r.GetInterviewerViewAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, "nobody@x.com", It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<InterviewerQCRowVM>());

            var result = await CreateService().GetInterviewerTimelineAsync("nobody@x.com", null, null);

            Assert.NotNull(result.Error);
            _repository.Verify(r => r.GetInterviewerTimelineAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task Timeline_combines_the_headline_row_irfs_and_events()
        {
            _repository
                .Setup(r => r.GetInterviewerViewAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, "a@x.com", It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<InterviewerQCRowVM> { new InterviewerQCRowVM { InterviewerEmail = "a@x.com", ActiveIrfs = 3 } });
            _repository
                .Setup(r => r.GetInterviewerTimelineAsync("a@x.com", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync((new List<InterviewerQCIrfVM> { new InterviewerQCIrfVM { IrfName = "IRF1" } },
                               new List<InterviewerQCEventVM> { new InterviewerQCEventVM { Kind = "QC" }, new InterviewerQCEventVM { Kind = "AI" } }));

            var result = await CreateService().GetInterviewerTimelineAsync(" a@x.com ", null, null);

            Assert.Null(result.Error);
            Assert.Equal(3, result.Summary.ActiveIrfs);
            Assert.Single(result.Irfs);
            Assert.Equal(2, result.Events.Count);
            Assert.False(result.Truncated);
        }

        [Fact]
        public void Metric_whitelist_covers_every_key_the_page_and_procedures_use()
        {
            var expected = new[]
            {
                "ReportReviewed", "PenaltyApplied", "SlaApproved", "SlaNotApproved", "QCForm", "TrialKept", "TrialRemoved", "Trial",
                "AuditBackfill", "AuditKept", "AuditRemoved", "AuditActions", "TrmUnderReview", "TrmReviewed", "TrmRemoved", "Trm",
                "TiUnderReview", "TiReviewed", "TiRemoved", "Ti", "PlivoCall", "Coaching", "All",
                "ReportEdited", "Suspicious", "SuspiciousUndone", "Blocked", "Unblocked", "AccessRevoked", "AccessRestored",
                "Inactivated", "Reactivated", "PaymentHold", "PaymentHoldReleased", "DeductionReview",
                "StatusActions", "SuspiciousAll", "BlockedAll", "AccessAll", "ActiveAll", "PaymentHoldAll"
            };

            Assert.Equal(expected.OrderBy(k => k), QCActivityMetrics.Keys.OrderBy(k => k));
            Assert.All(expected, k => Assert.Equal(k, QCActivityMetrics.Normalize(k.ToLowerInvariant())));
        }

        [Theory]
        [InlineData("ReportEdited")]
        [InlineData("Suspicious")]
        [InlineData("SuspiciousUndone")]
        [InlineData("Blocked")]
        [InlineData("Unblocked")]
        [InlineData("AccessRevoked")]
        [InlineData("AccessRestored")]
        [InlineData("Inactivated")]
        [InlineData("Reactivated")]
        [InlineData("PaymentHold")]
        [InlineData("PaymentHoldReleased")]
        [InlineData("StatusActions")]
        [InlineData("SuspiciousAll")]
        [InlineData("BlockedAll")]
        [InlineData("AccessAll")]
        [InlineData("ActiveAll")]
        [InlineData("PaymentHoldAll")]
        public async Task Status_and_report_edit_metric_keys_are_accepted_and_passed_as_the_canonical_key(string metric)
        {
            _repository
                .Setup(r => r.GetDetailsAsync("qc@risebird.io", metric, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<QCActivityDetailRowVM> { new QCActivityDetailRowVM { Metric = metric, RefId = 7 } });

            var result = await CreateService().GetDetailsAsync("qc@risebird.io", metric.ToLowerInvariant(), null, null);

            Assert.Null(result.Error);
            Assert.Equal(metric, result.Metric);
            Assert.NotEqual(metric, result.MetricLabel);
            Assert.Equal(7, Assert.Single(result.Rows).RefId);
        }

        [Fact]
        public void Totals_sum_report_edits_and_status_changes_with_their_undos()
        {
            var rows = new[]
            {
                new QCActivitySummaryRowVM { ReportsEdited = 2, MarkedSuspicious = 1, Blocked = 1, AccessRevoked = 1, Inactivated = 1, PaymentHolds = 1, Unblocked = 1 },
                new QCActivitySummaryRowVM { ReportsEdited = 1, SuspiciousUndone = 1, AccessRestored = 1, Reactivated = 1, PaymentHoldsReleased = 1 }
            };

            var totals = QCActivityTotalsVM.From(rows);

            Assert.Equal(3, totals.ReportsEdited);
            Assert.Equal(5, totals.StatusChanges);
            Assert.Equal(5, totals.StatusUndos);
            Assert.Equal(5, rows[0].StatusChanges);
            Assert.Equal(1, rows[0].StatusUndos);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task Report_edit_with_an_id_of_zero_or_less_returns_null_without_querying(int id)
        {
            var result = await CreateService().GetReportEditAsync(id);

            Assert.Null(result);
            _repository.Verify(r => r.GetReportEditAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Report_edit_is_loaded_from_the_repository()
        {
            _repository
                .Setup(r => r.GetReportEditAsync(42))
                .ReturnsAsync(new ReportEditVM { Id = 42, InterviewId = 9, OldValue = "before", NewValue = "after", EditedBy = "qc@risebird.io" });

            var result = await CreateService().GetReportEditAsync(42);

            Assert.Equal(42, result.Id);
            Assert.Equal("before", result.OldValue);
            Assert.Equal("after", result.NewValue);
        }

        [Fact]
        public async Task Report_edit_that_does_not_exist_returns_null()
        {
            _repository.Setup(r => r.GetReportEditAsync(5)).ReturnsAsync((ReportEditVM)null);

            Assert.Null(await CreateService().GetReportEditAsync(5));
        }

        [Fact]
        public async Task Report_edit_failure_is_logged_and_rethrown()
        {
            _repository.Setup(r => r.GetReportEditAsync(It.IsAny<int>())).ThrowsAsync(new InvalidOperationException("boom"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().GetReportEditAsync(1));
            _logger.Verify(l => l.LogMessage(LogLevelInfo.Exception, It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
        }
    }
}
