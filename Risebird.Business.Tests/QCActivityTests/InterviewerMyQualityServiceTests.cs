using Moq;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.QCActivityTests
{
    public class InterviewerMyQualityServiceTests
    {
        private static readonly DateTime NowUtc = new DateTime(2026, 10, 14, 6, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime CurFromIst = new DateTime(2026, 9, 15);

        private static readonly IReadOnlyDictionary<string, InterviewerFeedbackTextVM> NoWording =
            InterviewerMyQualityService.WordingMap(null);

        private static ExposureRiskRowVM Risk(int id, string breakdown, int day = 10) => new ExposureRiskRowVM
        {
            InterviewId = id,
            IrfId = 5,
            IrfName = "APEIOSMOBD",
            InterviewDateIst = new DateTime(2026, 10, day),
            SeverityBreakdown = breakdown
        };

        // ---------- Ranking ----------

        [Fact]
        public void RankFocus_RanksByCountTimesSeverityWeight_AndKeepsTopThree()
        {
            var issues = new[]
            {
                // Join on time: 2 interviews, worst P4 (weight 2) -> 4
                Risk(1, "InterviewerLateJoin:P4"),
                Risk(2, "InterviewerLateJoin:P5"),
                // Report quality: 1 interview, P0 (weight 6) -> 6
                Risk(3, "LessRejectComment:P0"),
                // Questions: 3 interviews, P5 (weight 1) -> 3
                Risk(4, "LessQuestions:P5"),
                Risk(5, "LessQuestions:P5"),
                Risk(6, "LessQuestions:P5"),
                // Camera: 1 interview, P5 -> 1 (drops out of the top 3)
                Risk(7, "InterviewerVideoOff:P5"),
            };

            var top = InterviewerMyQualityService.RankFocus(issues, null, CurFromIst, NoWording);

            Assert.Equal(3, top.Count);
            Assert.Equal(new[]
            {
                InterviewerCoachingBriefBuilder.ThemeReportQuality,
                InterviewerCoachingBriefBuilder.ThemeJoinOnTime,
                InterviewerCoachingBriefBuilder.ThemeQuestions
            }, top.Select(t => t.ThemeKey));
            Assert.Equal(2, top[1].Count);
            Assert.Equal("Write fuller feedback when you select / reject", top[0].Title);
        }

        [Fact]
        public void RankFocus_CountsDistinctInterviews_AndAddsPaymentReasonsInWindow()
        {
            var issues = new[] { Risk(1, "InterviewerLateJoin:P3"), Risk(1, "InterviewerLateJoin:P3") };
            var deductions = new[]
            {
                new MyQualityDeductionRowVM { InterviewId = 2, InterviewDateIst = new DateTime(2026, 10, 1), PaymentDeduction = 5, AttendanceJoinCompliance = "Yes" },
                // Older than the 30-day window: ignored for the top 3.
                new MyQualityDeductionRowVM { InterviewId = 3, InterviewDateIst = new DateTime(2026, 8, 20), PaymentDeduction = 5, AttendanceJoinCompliance = "Yes" }
            };

            var top = InterviewerMyQualityService.RankFocus(issues, deductions, CurFromIst, NoWording);

            var join = Assert.Single(top);
            Assert.Equal(InterviewerCoachingBriefBuilder.ThemeJoinOnTime, join.ThemeKey);
            Assert.Equal(2, join.Count);
            Assert.Equal(new[] { 1, 2 }, join.Evidence.Select(e => e.InterviewId).OrderBy(x => x));
        }

        [Fact]
        public void RankFocus_UsesFeedbackTextWhenPresent()
        {
            var wording = InterviewerMyQualityService.WordingMap(new[]
            {
                new InterviewerFeedbackTextVM { ThemeKey = "JoinOnTime", Title = "Be on time", WhatToDo = "Set a reminder." }
            });

            var top = InterviewerMyQualityService.RankFocus(new[] { Risk(1, "InterviewerLateJoin:P2") }, null, CurFromIst, wording);

            Assert.Equal("Be on time", top[0].Title);
            Assert.Equal("Set a reminder.", top[0].WhatToDo);
        }

        // ---------- Never shown ----------

        [Fact]
        public void RankFocus_NeverShowsFraudSuspiciousOrNoShowChecks()
        {
            var issues = new[]
            {
                Risk(1, "Interviewer No Show:P0"),
                Risk(2, "No Show:P0"),
                Risk(3, "Suspicious Proxy:P0"),
                Risk(4, "FakeReport:P0"),
                Risk(5, "KycMismatch:P0"),
                Risk(6, "SomeUnknownInternalCheck:P0"),
            };

            var top = InterviewerMyQualityService.RankFocus(issues, null, CurFromIst, NoWording);

            Assert.Empty(top);
        }

        [Fact]
        public void Build_NeverShowsInternalThemesOrLabels()
        {
            var data = new InterviewerMyQualityDataVM
            {
                Issues = new[] { Risk(1, "Suspicious:P0,InterviewerLateJoin:P1") },
                Deductions = new[]
                {
                    new MyQualityDeductionRowVM { InterviewId = 9, InterviewDateIst = new DateTime(2026, 10, 2), PaymentDeduction = 10,
                                                  UnethicalPractices = "Proxy suspected", ApprovalState = 1, Amount = 50 }
                },
                Coaching = new[]
                {
                    new MyQualityCoachingRowVM { SessionId = 4, CreatedAtUtc = NowUtc, Channel = "Call", Themes = "ChannelStanding,JoinOnTime,Other" }
                }
            };

            var vm = InterviewerMyQualityService.Build(data, NowUtc);

            var focus = Assert.Single(vm.TopFocus);
            Assert.Equal(InterviewerCoachingBriefBuilder.ThemeJoinOnTime, focus.ThemeKey);
            Assert.DoesNotContain(vm.TopFocus, f => InterviewerMyQualityService.NeverShownThemes.Contains(f.ThemeKey));

            var deduction = Assert.Single(vm.Deductions);
            Assert.Equal("Report quality review", deduction.Reason);
            Assert.DoesNotContain("Proxy", deduction.Reason);

            var coaching = Assert.Single(vm.Coaching);
            Assert.Equal(new[] { "Join every interview on time" }, coaching.Topics);

            var allText = string.Join("|", vm.TopFocus.Select(f => f.Title + f.WhatToDo)
                .Concat(vm.Deductions.Select(d => d.Reason))
                .Concat(vm.Coaching.SelectMany(c => c.Topics)));
            foreach (var label in new[] { "P0", "P1", "P2", "High-Risk", "Remove", "Watch", "suspicious", "fraud" })
                Assert.DoesNotContain(label, allText, StringComparison.OrdinalIgnoreCase);
        }

        // ---------- Tiles, deductions, empty states ----------

        [Fact]
        public void Build_TilesComputePercentAndTrend()
        {
            var data = new InterviewerMyQualityDataVM
            {
                Tiles = new MyQualityTilesRowVM
                {
                    CurCompleted = 48, CurIssueFree = 34, PrevCompleted = 50, PrevIssueFree = 31,
                    CurJoinTracked = 48, CurOnTime = 42, PrevJoinTracked = 40, PrevOnTime = 38,
                    MonthDeductionCount = 2, PrevMonthDeductionCount = 6, ActiveChannels = 4, PausedChannels = 1
                }
            };

            var vm = InterviewerMyQualityService.Build(data, NowUtc);

            Assert.Equal(71, vm.IssueFree.Percent);
            Assert.Equal(62, vm.IssueFree.PreviousPercent);
            Assert.Equal("Up", vm.IssueFree.Trend);
            Assert.Equal(88, vm.OnTime.Percent);
            Assert.Equal("Down", vm.OnTime.Trend);
            Assert.Equal("Down", vm.DeductionTrend);
            Assert.Equal(4, vm.ActiveChannels);
            Assert.Equal(1, vm.PausedChannels);
        }

        [Fact]
        public void Build_DeductionStatusesAndReasons()
        {
            var data = new InterviewerMyQualityDataVM
            {
                Deductions = new[]
                {
                    new MyQualityDeductionRowVM { InterviewId = 1, InterviewDateIst = new DateTime(2026, 10, 3), PaymentDeduction = 5, ApprovalState = 1, Amount = 40, TimeManagement = "Yes" },
                    new MyQualityDeductionRowVM { InterviewId = 2, InterviewDateIst = new DateTime(2026, 10, 2), PaymentDeduction = 3, ApprovalState = 0, Amount = 20, Recommendation = "REDO" },
                    new MyQualityDeductionRowVM { InterviewId = 3, InterviewDateIst = new DateTime(2026, 10, 1), PaymentDeduction = 8, ApprovalState = 2 }
                }
            };

            var vm = InterviewerMyQualityService.Build(data, NowUtc);

            Assert.Equal(new[] { "Applied", "Being checked", "Not applied" }, vm.Deductions.Select(d => d.Status));
            Assert.Equal("Interview time was not used fully", vm.Deductions[0].Reason);
            Assert.Equal(40, vm.Deductions[0].Amount);
            Assert.Null(vm.Deductions[1].Amount);
            Assert.Equal("The interview needed to be redone", vm.Deductions[1].Reason);
        }

        // ---------- Deduction reasons ----------

        [Fact]
        public void DeductionReason_GivesAPlainReasonPerColumn_InsteadOfAdvice()
        {
            var d = new MyQualityDeductionRowVM
            {
                CandidateInteractionQuality = "Rude tone flagged by AI",
                WrongStatusUpdates = "Yes",
                LimitedHandoncoding = "No coding asked"
            };

            var reason = InterviewerMyQualityService.DeductionReason(d, NoWording);

            Assert.Equal("Too little hands-on coding; Interview status was updated incorrectly; Interaction with the candidate", reason);
            Assert.DoesNotContain("rate the experience", reason);
            Assert.DoesNotContain("Rude tone", reason);       // raw QC / AI flag text is never shown
        }

        [Fact]
        public void DeductionReason_EveryColumnHasItsOwnReason()
        {
            foreach (var c in InterviewerMyQualityService.DeductionReasonColumns)
            {
                var d = new MyQualityDeductionRowVM();
                typeof(ExposurePaymentRowVM).GetProperty(c.Column)!.SetValue(d, "Yes");
                Assert.Equal(c.Reason, InterviewerMyQualityService.DeductionReason(d, NoWording));
            }
            Assert.Equal(InterviewerMyQualityService.DeductionReasonColumns.Count,
                InterviewerMyQualityService.DeductionReasonColumns.Select(c => c.Reason).Distinct().Count());
        }

        [Fact]
        public void DeductionReason_HidesConduct_IgnoresNoValues_AddsRedo()
        {
            Assert.Equal(InterviewerMyQualityService.GenericDeductionReason,
                InterviewerMyQualityService.DeductionReason(new MyQualityDeductionRowVM { UnethicalPractices = "Proxy suspected" }, NoWording));
            Assert.Equal(InterviewerMyQualityService.GenericDeductionReason,
                InterviewerMyQualityService.DeductionReason(new MyQualityDeductionRowVM { TimeManagement = "No", InterviewerVideo = "Video Off When Mandatory" }, NoWording));
            Assert.Equal("Joined the interview late; The interview needed to be redone",
                InterviewerMyQualityService.DeductionReason(new MyQualityDeductionRowVM { AttendanceJoinCompliance = "Yes", Recommendation = "REDO" }, NoWording));
        }

        [Fact]
        public void DeductionReason_CanBeReworded_FromFeedbackText()
        {
            var wording = InterviewerMyQualityService.WordingMap(new[]
            {
                new InterviewerFeedbackTextVM { ThemeKey = "Reason.TimeManagement", Title = "Interview ended early", WhatToDo = "-" }
            });

            Assert.Equal("Interview ended early",
                InterviewerMyQualityService.DeductionReason(new MyQualityDeductionRowVM { TimeManagement = "Yes" }, wording));
        }

        [Fact]
        public void Build_EmptyData_GivesEmptyStates()
        {
            var vm = InterviewerMyQualityService.Build(new InterviewerMyQualityDataVM(), NowUtc);

            Assert.Null(vm.IssueFree.Percent);
            Assert.Null(vm.OnTime.Percent);
            Assert.Null(vm.DeductionTrend);
            Assert.Empty(vm.TopFocus);
            Assert.Empty(vm.Deductions);
            Assert.Empty(vm.Channels);
            Assert.Empty(vm.Coaching);
            Assert.Null(vm.Error);

            var fromNull = InterviewerMyQualityService.Build(null, NowUtc);
            Assert.Empty(fromNull.TopFocus);
        }

        [Fact]
        public void DefaultWording_CoversEveryThemeKey()
        {
            foreach (var key in InterviewerCoachingBriefBuilder.ThemeKeys)
                Assert.True(InterviewerMyQualityService.DefaultWording.ContainsKey(key), key);
        }

        // ---------- Service ----------

        [Fact]
        public async Task GetAsync_RepositoryFailure_ReturnsErrorAndLogs()
        {
            var repo = new Mock<IInterviewerMyQualityRepository>();
            repo.Setup(r => r.GetAsync(It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("boom"));
            var logger = new Mock<IApplicationErrorLogger>();
            var service = new InterviewerMyQualityService(repo.Object, logger.Object, () => NowUtc);

            var vm = await service.GetAsync("iv@example.com", null);

            Assert.False(string.IsNullOrEmpty(vm.Error));
            logger.Verify(l => l.LogMessage(LogLevelInfo.Exception, It.IsAny<Exception>()), Times.Once);
        }

        [Fact]
        public async Task GetAsync_UsesSessionEmailWithoutQuotes()
        {
            var repo = new Mock<IInterviewerMyQualityRepository>();
            repo.Setup(r => r.GetAsync("iv@example.com")).ReturnsAsync(new InterviewerMyQualityDataVM());
            var service = new InterviewerMyQualityService(repo.Object, Mock.Of<IApplicationErrorLogger>(), () => NowUtc);

            var vm = await service.GetAsync("\"iv@example.com\"", new InterviewerInsightsVM { CompletedInterviews = 10, SelectedInterviews = 4, RejectedInterviews = 6 });

            repo.Verify(r => r.GetAsync("iv@example.com"), Times.Once);
            Assert.Equal(40, vm.Record.SelectPercent);
            Assert.Equal(60, vm.Record.RejectPercent);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(0, true)]
        [InlineData(-1, false)]
        [InlineData(-2, false)]
        public async Task AcknowledgeCoachingAsync_MapsResults(int repoResult, bool ok)
        {
            var repo = new Mock<IInterviewerMyQualityRepository>();
            repo.Setup(r => r.AcknowledgeCoachingAsync(7, "iv@example.com")).ReturnsAsync(repoResult);
            var service = new InterviewerMyQualityService(repo.Object, Mock.Of<IApplicationErrorLogger>(), () => NowUtc);

            var result = await service.AcknowledgeCoachingAsync(7, "iv@example.com");

            Assert.Equal(ok, result.Ok);
        }

        [Fact]
        public async Task AcknowledgeCoachingAsync_InvalidInput_DoesNotCallRepository()
        {
            var repo = new Mock<IInterviewerMyQualityRepository>();
            var service = new InterviewerMyQualityService(repo.Object, Mock.Of<IApplicationErrorLogger>(), () => NowUtc);

            var result = await service.AcknowledgeCoachingAsync(0, "iv@example.com");

            Assert.False(result.Ok);
            repo.Verify(r => r.AcknowledgeCoachingAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }
    }
}
