using Moq;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.QCActivityTests
{
    /// <summary>
    /// QC Tracking and High-Risk Interviewers: Potential Risk tabs (Delta 3), page attribution helpers (Deltas 1, 4),
    /// overall-level trend (Delta 4), PLI status on rows (Delta 5) and the QC Activity page columns.
    /// </summary>
    public class QCTrackingHighRiskTests
    {
        private static MeetingVM Row(int userId, string email, int? status = null, bool? active = true) =>
            new MeetingVM { InterviewerUserId = userId, interviewer = email, PotentialRiskStatus = status, InterviewerIsActive = active };

        // ---------- Delta 3: Potential Risk by Candidate tabs ----------

        [Theory]
        [InlineData(null, true, PotentialRiskTabs.Pending)]
        [InlineData(1, true, PotentialRiskTabs.UnderReview)]
        [InlineData(2, true, PotentialRiskTabs.Reviewed)]
        [InlineData(3, true, PotentialRiskTabs.Removed)]
        [InlineData(null, false, PotentialRiskTabs.Removed)]   // made inactive elsewhere
        [InlineData(2, false, PotentialRiskTabs.Removed)]
        [InlineData(null, null, PotentialRiskTabs.Pending)]    // older procedure: treated as active
        public void A_row_belongs_to_the_tab_of_its_interviewers_decision(int? status, bool? active, string expected)
        {
            Assert.Equal(expected, PotentialRiskTabs.TabOf(Row(5, "a@x.com", status, active)));
        }

        [Theory]
        [InlineData(null, "All")]
        [InlineData("", "All")]
        [InlineData("garbage", "All")]
        [InlineData("pending", "Pending")]
        [InlineData("UNDERREVIEW", "UnderReview")]
        [InlineData("Removed", "Removed")]
        public void An_unknown_tab_falls_back_to_All(string tab, string expected)
        {
            Assert.Equal(expected, PotentialRiskTabs.Normalize(tab));
        }

        [Fact]
        public void Tab_counts_are_distinct_interviewers_and_all_of_an_interviewers_rows_move_together()
        {
            var rows = new List<MeetingVM>
            {
                Row(1, "a@x.com"), Row(1, "a@x.com"), Row(1, "a@x.com"),   // one interviewer, three reports
                Row(2, "b@x.com", status: 1),
                Row(3, "c@x.com", status: 2),
                Row(4, "d@x.com", active: false)
            };

            var counts = PotentialRiskTabs.Counts(rows).ToDictionary(c => c.Tab);

            Assert.Equal(4, counts["All"].Interviewers);
            Assert.Equal(6, counts["All"].Rows);
            Assert.Equal(1, counts["Pending"].Interviewers);
            Assert.Equal(3, counts["Pending"].Rows);
            Assert.Equal(1, counts["UnderReview"].Interviewers);
            Assert.Equal(1, counts["Reviewed"].Interviewers);
            Assert.Equal(1, counts["Removed"].Interviewers);
            // With no decisions, Pending equals All minus inactive.
            Assert.Equal(counts["All"].Interviewers - counts["Removed"].Interviewers - counts["UnderReview"].Interviewers - counts["Reviewed"].Interviewers,
                counts["Pending"].Interviewers);

            Assert.Equal(3, PotentialRiskTabs.Filter(rows, "Pending").Count());
            Assert.Equal(6, PotentialRiskTabs.Filter(rows, "nonsense").Count());

            // Under review on that interviewer: all three rows leave Pending.
            foreach (var r in rows.Where(r => r.InterviewerUserId == 1)) r.PotentialRiskStatus = 1;
            Assert.Empty(PotentialRiskTabs.Filter(rows, "Pending"));
            Assert.Equal(4, PotentialRiskTabs.Filter(rows, "UnderReview").Count());
        }

        // ---------- Delta 1 / 4: page attribution ----------

        [Theory]
        [InlineData("PotentailRiskCandidate", "PotentialRisk")]
        [InlineData("HighRisk", "HighRisk")]
        [InlineData("CompletedInterview", null)]
        [InlineData(null, null)]
        public void Payment_SLA_saves_from_Potential_Risk_and_High_Risk_name_their_page(string pagetype, string expected)
        {
            Assert.Equal(expected, PaymentSLAVM.QcSourcePageFor(pagetype));
        }

        [Theory]
        [InlineData("PotentialRiskRCandidate", "PotentialRisk")]
        [InlineData("HighRisk", "HighRisk")]
        [InlineData("Completed", null)]
        public void Redo_requests_from_Potential_Risk_and_High_Risk_name_their_page(string type, string expected)
        {
            Assert.Equal(expected, RedoInterviewVM.QcSourcePageFor(type));
        }

        [Fact]
        public void Report_filters_keep_known_keys_only()
        {
            Assert.Equal("NotReviewed,P0P1", HighRiskReportFilters.Normalize("notreviewed, p0p1, drop table, NotReviewed"));
            Assert.Null(HighRiskReportFilters.Normalize("nothing,known"));
            Assert.Null(HighRiskReportFilters.Normalize(null));
        }

        // ---------- Delta 4: overall level ----------

        [Theory]
        [InlineData(10, 5, 10, 2, "up")]      // 20% -> 50%: worse
        [InlineData(10, 2, 10, 5, "down")]    // 50% -> 20%: coaching worked
        [InlineData(10, 2, 5, 1, "flat")]     // 20% -> 20%
        [InlineData(10, 2, 0, 0, null)]       // no interviews before
        public void Trend_compares_bad_percent_with_the_previous_window(int completed, int bad, int prevCompleted, int prevBad, string expected)
        {
            var row = new InterviewerRiskOverviewRowVm { TotalCompleted = completed, TotalBad = bad, PrevCompleted = prevCompleted, PrevBad = prevBad };
            Assert.Equal(expected, row.Trend);
        }

        // ---------- Delta 5: PLI decision on each row ----------

        [Fact]
        public void PLI_rows_take_the_later_of_the_channel_decision_and_an_interviewer_wide_remove()
        {
            var rows = new List<PQIDetails>
            {
                new PQIDetails { UserId = 7, id = 100 },
                new PQIDetails { UserId = 7, id = 200 },
                new PQIDetails { UserId = 8, id = 100 }
            };
            var statuses = new List<QcPageStatusVM>
            {
                new QcPageStatusVM { UserId = 7, IRFId = 100, Status = 1, CreatedBy = "qc@x.com", CreatedOn = new DateTime(2026, 10, 1) },
                new QcPageStatusVM { UserId = 7, IRFId = 0, Status = 3, CreatedBy = "lead@x.com", CreatedOn = new DateTime(2026, 10, 5) }
            };

            QcPageStatusVM.ApplyPli(rows, statuses);

            Assert.Equal(3, rows[0].PliStatus);              // removed later, overrides the channel's under review
            Assert.Equal("lead@x.com", rows[0].PliStatusBy);
            Assert.Equal(3, rows[1].PliStatus);              // removal applies to every channel
            Assert.Null(rows[2].PliStatus);                  // another interviewer: no decision
        }

        // ---------- QC Activity: the new page columns are part of Actions taken ----------

        [Fact]
        public void Actions_taken_includes_Potential_Risk_High_Risk_and_Monitoring()
        {
            var row = new QCActivitySummaryRowVM
            {
                ActionsTotalInterviews = 1, ActionsTrm = 1, ActionsIrfAudit = 1, ActionsTrial = 1, ActionsSuspicious = 1, ActionsReportsSla = 1,
                ActionsPotentialRisk = 2, ActionsHighRisk = 3, ActionsMonitoring = 4
            };
            Assert.Equal(15, row.ActionsTaken);
        }

        [Theory]
        [InlineData("PagePotentialRisk")]
        [InlineData("PageHighRisk")]
        [InlineData("PageMonitoring")]
        [InlineData("Pr")]
        [InlineData("Hr")]
        [InlineData("MonitoringRemoved")]
        public void New_drill_down_keys_are_whitelisted(string key)
        {
            Assert.True(QCActivityMetrics.IsValid(key));
            Assert.Equal(key, QCActivityMetrics.Normalize(key.ToUpperInvariant()));
        }

        [Fact]
        public async Task The_summary_service_passes_the_new_page_columns_through()
        {
            var repository = new Mock<IQCActivityDashboardRepository>();
            var logger = new Mock<IApplicationErrorLogger>();
            IReadOnlyList<QCActivitySummaryRowVM> summaryRows = new List<QCActivitySummaryRowVM>
            {
                new QCActivitySummaryRowVM { ActorEmail = "qc@x.com", ActionsPotentialRisk = 2, PrUnderReview = 1, PrRemoved = 1, ActionsHighRisk = 1, HrReviewed = 1 }
            };
            repository.Setup(r => r.GetSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync((summaryRows, new QCActivityBacklogVM()));
            var sut = new QCActivityDashboardService(repository.Object, logger.Object, () => new DateTime(2026, 10, 16));

            var data = await sut.GetSummaryAsync(null, null, true, null);

            var row = Assert.Single(data.Rows);
            Assert.Equal(2, row.ActionsPotentialRisk);
            Assert.Equal(1, row.HrReviewed);
            Assert.Equal(3, data.Totals.ActionsTaken);
        }
    }
}
