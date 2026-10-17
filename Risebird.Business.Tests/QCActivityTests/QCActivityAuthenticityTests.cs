using Moq;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.QCActivityTests
{
    /// <summary>QC Activity part 2: authentic QC work (authentic-action rule, flag thresholds from settings, own-row restriction).</summary>
    public class QCActivityAuthenticityTests
    {
        private static readonly DateTime TodayIst = new DateTime(2026, 10, 14);

        private readonly Mock<IQCActivityDashboardRepository> _repository = new();
        private readonly Mock<IApplicationErrorLogger> _logger = new();

        private QCActivityDashboardService CreateService() =>
            new QCActivityDashboardService(_repository.Object, _logger.Object, () => TodayIst);

        // ---------- Authentic-action rule ----------

        [Theory]
        [InlineData("Called the interviewer, agreed to join on time", true, true)]
        [InlineData("Called the interviewer, agreed to join on time", false, false)]   // no evidence
        [InlineData("ok", true, false)]                                                 // stock word
        [InlineData("Done.", true, false)]                                              // stock word with punctuation
        [InlineData("Short comment", true, false)]                                      // under 20 characters
        [InlineData(null, true, false)]                                                 // no comment
        public void Authentic_action_needs_a_real_comment_and_evidence(string comment, bool hasEvidence, bool expected)
        {
            var settings = new QCActivitySettingsVM();
            Assert.Equal(expected, settings.IsAuthenticAction(comment, hasEvidence));
        }

        [Fact]
        public void Not_authentic_reason_names_every_missing_part()
        {
            var settings = new QCActivitySettingsVM();

            Assert.Equal("stock comment (\"ok\"); no evidence on the interviewer in the window", settings.NotAuthenticReason("ok", false));
            Assert.Equal("comment under 20 characters", settings.NotAuthenticReason("too short", true));
            Assert.Equal("no comment", settings.NotAuthenticReason("  ", true));
            Assert.Null(settings.NotAuthenticReason("Called and explained the rubric scoring", true));
        }

        [Theory]
        [InlineData(180, "completed", true)]
        [InlineData(120, "completed | NORMAL_CLEARING", true)]
        [InlineData(119, "completed", false)]       // under MinCallSeconds
        [InlineData(300, "busy | USER_BUSY", false)]
        [InlineData(300, "no-answer", false)]
        [InlineData(null, "completed", false)]      // no talk time yet
        public void Connected_call_is_answered_and_long_enough(int? seconds, string status, bool expected)
        {
            Assert.Equal(expected, new QCActivitySettingsVM().IsConnectedCall(seconds, status));
        }

        // ---------- Thresholds read from settings ----------

        [Fact]
        public void Settings_are_read_from_the_table_and_bad_rows_keep_their_defaults()
        {
            var s = QCActivitySettingsVM.From(new[]
            {
                new QCActivitySettingRowVM { Name = "MinCallSeconds", Value = "90" },
                new QCActivitySettingRowVM { Name = "MinCommentChars", Value = "30" },
                new QCActivitySettingRowVM { Name = "BurstCount", Value = "5" },
                new QCActivitySettingRowVM { Name = "BurstMinutes", Value = "not a number" },
                new QCActivitySettingRowVM { Name = "ReversalDays", Value = "3" },
                new QCActivitySettingRowVM { Name = "ShortCallSeconds", Value = "45" },
                new QCActivitySettingRowVM { Name = "ShortCallPct", Value = "60" },
                new QCActivitySettingRowVM { Name = "StockComments", Value = "OK, Done ,looked" }
            });

            Assert.Equal(90, s.MinCallSeconds);
            Assert.Equal(30, s.MinCommentChars);
            Assert.Equal(5, s.BurstCount);
            Assert.Equal(10, s.BurstMinutes);               // invalid value -> default
            Assert.Equal(3, s.ReversalDays);
            Assert.Equal(45, s.ShortCallSeconds);
            Assert.Equal(60, s.ShortCallPct);
            Assert.True(s.IsStockComment("looked"));
            Assert.False(s.IsStockComment("fine"));         // replaced list
            Assert.True(s.IsConnectedCall(90, "completed"));
            Assert.False(s.IsAuthenticAction("Twenty five characters...", true));   // under 30
        }

        [Fact]
        public async Task Flag_rules_show_the_thresholds_in_force()
        {
            _repository
                .Setup(r => r.GetFlagsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
                .ReturnsAsync((
                    new List<QCActivityFlagVM>
                    {
                        new QCActivityFlagVM { Flag = "Burst", ActorEmail = "qc@risebird.io", Count = 12, Metrics = "TrmReviewed" },
                        new QCActivityFlagVM { Flag = "ShortCalls", ActorEmail = "qc@risebird.io", Count = 4, Metrics = "PlivoCall" },
                        new QCActivityFlagVM { Flag = "ReversedByOther", ActorEmail = "qc@risebird.io", Count = 1 }
                    },
                    new List<QCActivityFlagItemVM>
                    {
                        new QCActivityFlagItemVM { Flag = "Burst", ActorEmail = "qc@risebird.io", Metric = "TrmReviewed", InterviewId = 1 },
                        new QCActivityFlagItemVM { Flag = "Burst", ActorEmail = "qc@risebird.io", Metric = "TiReviewed", InterviewId = 2 },
                        new QCActivityFlagItemVM { Flag = "ShortCalls", ActorEmail = "qc@risebird.io", Metric = "PlivoCall" }
                    },
                    new List<QCActivitySettingRowVM>
                    {
                        new QCActivitySettingRowVM { Name = "BurstCount", Value = "6" },
                        new QCActivitySettingRowVM { Name = "BurstMinutes", Value = "15" },
                        new QCActivitySettingRowVM { Name = "ShortCallSeconds", Value = "40" },
                        new QCActivitySettingRowVM { Name = "ShortCallPct", Value = "70" },
                        new QCActivitySettingRowVM { Name = "ReversalDays", Value = "5" }
                    }));

            var result = await CreateService().GetFlagsAsync(null, null, null);

            Assert.Null(result.Error);
            Assert.Equal(6, result.Settings.BurstCount);
            var burst = Assert.Single(result.Flags, f => f.Flag == "Burst");
            Assert.Equal("6 or more actions of the same type within 15 minutes", burst.Rule);
            Assert.Equal(new[] { 1 }, burst.Items.Select(i => i.InterviewId.Value));   // only the burst's own metric
            Assert.Contains("70%", result.Flags.Single(f => f.Flag == "ShortCalls").Rule);
            Assert.Contains("40 seconds", result.Flags.Single(f => f.Flag == "ShortCalls").Rule);
            Assert.Contains("5 days", result.Flags.Single(f => f.Flag == "ReversedByOther").Rule);
        }

        // ---------- Own-row restriction for non-managers ----------

        [Fact]
        public async Task Restricted_viewer_only_gets_their_own_flags()
        {
            _repository
                .Setup(r => r.GetFlagsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), "qc@risebird.io"))
                .ReturnsAsync((
                    new List<QCActivityFlagVM>
                    {
                        new QCActivityFlagVM { Flag = "Burst", ActorEmail = "qc@risebird.io", Count = 10 },
                        new QCActivityFlagVM { Flag = "Burst", ActorEmail = "other@risebird.io", Count = 15 }
                    },
                    new List<QCActivityFlagItemVM>(),
                    new List<QCActivitySettingRowVM>()));

            // A Quality Controller asking for someone else is narrowed to their own email.
            var result = await CreateService().GetFlagsAsync("other@risebird.io", null, null, restrictToActorEmail: " qc@risebird.io ");

            var flag = Assert.Single(result.Flags);
            Assert.Equal("qc@risebird.io", flag.ActorEmail);
            Assert.Equal("qc@risebird.io", result.ActorEmail);
            _repository.Verify(r => r.GetFlagsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), "qc@risebird.io"), Times.Once);
        }

        [Fact]
        public async Task Flags_failure_returns_an_error_and_logs()
        {
            _repository
                .Setup(r => r.GetFlagsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var result = await CreateService().GetFlagsAsync(null, null, null);

            Assert.False(string.IsNullOrEmpty(result.Error));
            Assert.Empty(result.Flags);
            _logger.Verify(l => l.LogMessage(Risebird.Entities.Enum.LogLevelInfo.Exception, It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
        }

        // ---------- Summary / details / coverage plumbing ----------

        [Fact]
        public async Task Summary_passes_coverage_through_and_scores_people()
        {
            var coverage = new QCActivityCoverageVM
            {
                HighRiskTotal = 10,
                HighRiskTouched = 7,
                CoveragePct = 70m,
                Untouched = new[] { new QCActivityUntouchedVM { InterviewerId = 5, WorstSeverity = "P0" } }
            };
            _repository
                .Setup(r => r.GetSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<QCActivitySummaryRowVM>
                {
                    new QCActivitySummaryRowVM { ActorEmail = "a@risebird.io", ScoredActions = 8, AuthenticActions = 6, ActionsTrm = 5, ActionsTotalInterviews = 3,
                                                 ConnectedCalls = 2, ConnectedTalkSeconds = 600, FollowThroughOf = 4, FollowThroughDone = 3, FlagCount = 1 },
                    new QCActivitySummaryRowVM { ActorEmail = "b@risebird.io", ScoredActions = 2, AuthenticActions = 0 }
                }, new QCActivityBacklogVM { Coverage = coverage }));

            var result = await CreateService().GetSummaryAsync(null, null, true);

            Assert.Equal(70m, result.Coverage.CoveragePct);
            Assert.Single(result.Coverage.Untouched);
            var a = result.Rows[0];
            Assert.Equal(75, a.AuthenticPct);
            Assert.Equal(8, a.ActionsTaken);
            Assert.Equal(75, a.FollowThroughPct);
            Assert.Equal(300, a.ConnectedAvgCallSeconds);
            Assert.Null(result.Rows[1].FollowThroughPct);
            Assert.Equal(60, result.Totals.AuthenticPct);      // 6 of 10
            Assert.Equal(1, result.Totals.FlagCount);
        }

        [Fact]
        public async Task Details_say_why_an_action_is_not_authentic()
        {
            _repository
                .Setup(r => r.GetDetailsAsync("qc@risebird.io", "NotAuthentic", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<QCActivityDetailRowVM>
                {
                    new QCActivityDetailRowVM { Metric = "TrmReviewed", Comment = "ok", HasEvidence = true, IsAuthentic = false },
                    new QCActivityDetailRowVM { Metric = "PlivoCall", IsAuthentic = false },
                    new QCActivityDetailRowVM { Metric = "TiReviewed", Comment = "Explained the late joins on a call", HasEvidence = true, IsAuthentic = true }
                });
            _repository.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new List<QCActivitySettingRowVM>());

            var result = await CreateService().GetDetailsAsync("qc@risebird.io", "notauthentic", null, null);

            Assert.Equal("stock comment (\"ok\")", result.Rows[0].NotAuthenticReason);
            Assert.Null(result.Rows[1].NotAuthenticReason);   // calls are not explained this way
            Assert.Null(result.Rows[2].NotAuthenticReason);
        }

        [Fact]
        public async Task Coverage_failure_returns_an_error_object()
        {
            _repository.Setup(r => r.GetCoverageAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var result = await CreateService().GetCoverageAsync(null, null);

            Assert.False(string.IsNullOrEmpty(result.Error));
            Assert.Empty(result.Untouched);
        }

        [Fact]
        public void Every_flag_key_has_a_title_and_rule()
        {
            foreach (var key in new[] { "Burst", "ReviewedNoCall", "ReversedByOther", "ShortCalls", "KeepDespiteP0", "NoEvidence" })
            {
                var (title, rule) = QCActivityDashboardService.DescribeFlag(key, new QCActivitySettingsVM());
                Assert.NotEqual(key, title);
                Assert.False(string.IsNullOrWhiteSpace(rule));
            }
        }
    }
}
