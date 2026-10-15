using Moq;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.QCActivityTests
{
    public class DeductionReviewServiceTests
    {
        // Wednesday 14 Oct 2026, 11:30 IST.
        private static readonly DateTime NowUtc = new DateTime(2026, 10, 14, 6, 0, 0, DateTimeKind.Utc);
        private static readonly DeductionReviewSettings Rules = new DeductionReviewSettings();

        private static DeductionReviewService Service(Mock<IDeductionReviewRepository> repo, Mock<INotificationHelper> mail = null,
            DeductionReviewSettings settings = null) =>
            new DeductionReviewService(repo.Object, (mail ?? new Mock<INotificationHelper>()).Object,
                Mock.Of<IApplicationErrorLogger>(), settings ?? Rules, () => NowUtc);

        private static string Note(int length) => new string('x', length);

        // ---------- Working-day deadline ----------

        [Fact]
        public void AddWorkingDays_SkipsTheWeekend()
        {
            // Wed + 5 working days = next Wed (Sat/Sun skipped).
            var due = DeductionReviewConstants.AddWorkingDaysUtc(NowUtc, 5);
            Assert.Equal(new DateTime(2026, 10, 21), due.AddMinutes(330).Date);

            // Friday + 1 working day = Monday.
            var friday = new DateTime(2026, 10, 16, 6, 0, 0, DateTimeKind.Utc);
            Assert.Equal(DayOfWeek.Monday, DeductionReviewConstants.AddWorkingDaysUtc(friday, 1).AddMinutes(330).DayOfWeek);
        }

        // ---------- Interviewer request ----------

        [Fact]
        public async Task RequestReview_ValidatesReasonAndNoteBeforeSaving()
        {
            var repo = new Mock<IDeductionReviewRepository>();
            var service = Service(repo);

            Assert.False((await service.RequestReviewAsync("iv@example.com", new DeductionReviewRequestInputVM { InterviewId = 5, ReasonCategory = "Nope", Note = Note(40) })).Ok);
            Assert.False((await service.RequestReviewAsync("iv@example.com", new DeductionReviewRequestInputVM { InterviewId = 5, ReasonCategory = "Technical", Note = Note(29) })).Ok);
            Assert.False((await service.RequestReviewAsync("iv@example.com", new DeductionReviewRequestInputVM { InterviewId = 5, ReasonCategory = "Technical", Note = Note(501) })).Ok);
            Assert.False((await service.RequestReviewAsync(null, new DeductionReviewRequestInputVM { InterviewId = 5, ReasonCategory = "Technical", Note = Note(40) })).Ok);

            repo.Verify(r => r.SaveRequestAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DeductionReviewSettings>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task RequestReview_PassesSessionEmailWindowAndDeadline()
        {
            var repo = new Mock<IDeductionReviewRepository>();
            repo.Setup(r => r.SaveRequestAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<DeductionReviewSettings>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync((1, 12));
            var service = Service(repo);

            var result = await service.RequestReviewAsync("\"iv@example.com\"",
                new DeductionReviewRequestInputVM { InterviewId = 5, ReasonCategory = "technical", Note = "  " + Note(30) + "  " });

            Assert.True(result.Ok);
            repo.Verify(r => r.SaveRequestAsync("iv@example.com", 5, "Technical", Note(30), Rules,
                NowUtc.AddMinutes(330).AddDays(-7),
                It.Is<DateTime>(d => d.AddMinutes(330).Date == new DateTime(2026, 10, 21))), Times.Once);
        }

        [Theory]
        [InlineData(-2, "3%")]
        [InlineData(-3, "7 days")]
        [InlineData(-4, "3 review requests")]
        [InlineData(-5, "already")]
        [InlineData(-6, "not been applied")]
        public async Task RequestReview_MapsRuleFailures(int code, string expectedText)
        {
            var repo = new Mock<IDeductionReviewRepository>();
            repo.Setup(r => r.SaveRequestAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<DeductionReviewSettings>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync((code, (int?)null));

            var result = await Service(repo).RequestReviewAsync("iv@example.com",
                new DeductionReviewRequestInputVM { InterviewId = 5, ReasonCategory = "Other", Note = Note(40) });

            Assert.False(result.Ok);
            Assert.Contains(expectedText, result.Message);
        }

        // ---------- QC decision ----------

        [Fact]
        public async Task Decide_RequiresReasonAndPercentForReduce()
        {
            var repo = new Mock<IDeductionReviewRepository>();
            var service = Service(repo);

            Assert.False((await service.DecideAsync("qc@example.com", new DeductionReviewDecisionInputVM { RequestId = 1, Decision = "Refund", Note = "" })).Ok);
            Assert.False((await service.DecideAsync("qc@example.com", new DeductionReviewDecisionInputVM { RequestId = 1, Decision = "Reduce", Note = "Audio drop confirmed" })).Ok);
            Assert.False((await service.DecideAsync("qc@example.com", new DeductionReviewDecisionInputVM { RequestId = 1, Decision = "Maybe", Note = "Audio drop confirmed" })).Ok);

            repo.Verify(r => r.DecideAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Decide_Reduce_PassesPercent_AndWarnsWhenAlreadyPaid()
        {
            var repo = new Mock<IDeductionReviewRepository>();
            repo.Setup(r => r.DecideAsync(3, "Reduce", 2, "Audio drop confirmed", "qc@example.com"))
                .ReturnsAsync(new DeductionReviewDecideResultVM { Result = 1, Status = "Reduced", NewDeductionPct = 2, PayoutAlreadyPaid = true });

            var result = await Service(repo).DecideAsync("qc@example.com",
                new DeductionReviewDecisionInputVM { RequestId = 3, Decision = "reduce", NewDeductionPct = 2, Note = "Audio drop confirmed" });

            Assert.True(result.Ok);
            Assert.Contains("reduced to 2%", result.Message);
            Assert.Contains("Finance", result.Message);
        }

        [Fact]
        public async Task Decide_Stands_DoesNotSendAPercent()
        {
            var repo = new Mock<IDeductionReviewRepository>();
            repo.Setup(r => r.DecideAsync(3, "Stands", null, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new DeductionReviewDecideResultVM { Result = 1, Status = "Stands", NewDeductionPct = 5 });

            var result = await Service(repo).DecideAsync("qc@example.com",
                new DeductionReviewDecisionInputVM { RequestId = 3, Decision = "Stands", NewDeductionPct = 2, Note = "Report shows the late join" });

            Assert.True(result.Ok);
            repo.Verify(r => r.DecideAsync(3, "Stands", null, "Report shows the late join", "qc@example.com"), Times.Once);
        }

        [Theory]
        [InlineData(-2, "already been decided")]
        [InlineData(-3, "new %")]
        [InlineData(-1, "not found")]
        public async Task Decide_MapsFailures(int code, string expectedText)
        {
            var repo = new Mock<IDeductionReviewRepository>();
            repo.Setup(r => r.DecideAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new DeductionReviewDecideResultVM { Result = code });

            var result = await Service(repo).DecideAsync("qc@example.com",
                new DeductionReviewDecisionInputVM { RequestId = 3, Decision = "Refund", Note = "Platform outage" });

            Assert.False(result.Ok);
            Assert.Contains(expectedText, result.Message);
        }

        // ---------- AI accuracy by check ----------

        [Fact]
        public void BuildCheckStats_ComputesUpheldRate_AndSuggestsExclusion()
        {
            var stats = DeductionReviewService.BuildCheckStats(new[]
            {
                new DeductionReviewCheckStatVM { AiCheck = "LateJoin", Requests = 10, Decided = 8, Upheld = 6 },
                new DeductionReviewCheckStatVM { AiCheck = "ShortComment", Requests = 6, Decided = 6, Upheld = 1 },
                new DeductionReviewCheckStatVM { AiCheck = "Duration", Requests = 3, Decided = 3, Upheld = 3 },
                new DeductionReviewCheckStatVM { AiCheck = "CameraOff", Requests = 9, Decided = 9, Upheld = 9, ExcludeFromAutoApprove = true },
                new DeductionReviewCheckStatVM { AiCheck = "Questions", Requests = 1, Decided = 0, Upheld = 0, Open = 1 }
            }, Rules);

            var late = stats.Single(s => s.AiCheck == "LateJoin");
            Assert.Equal(75, late.UpheldRatePct);
            Assert.True(late.SuggestExclude);
            Assert.Equal("Late join", late.AiCheckLabel);

            Assert.False(stats.Single(s => s.AiCheck == "ShortComment").SuggestExclude);   // low rate stays automatic
            Assert.False(stats.Single(s => s.AiCheck == "Duration").SuggestExclude);       // too few decisions
            Assert.False(stats.Single(s => s.AiCheck == "CameraOff").SuggestExclude);      // already excluded
            Assert.Null(stats.Single(s => s.AiCheck == "Questions").UpheldRatePct);
            Assert.Equal("LateJoin", stats[0].AiCheck);
        }

        [Fact]
        public async Task SetCheckPolicy_RejectsUnknownChecks()
        {
            var repo = new Mock<IDeductionReviewRepository>();
            var service = Service(repo);

            Assert.False((await service.SetCheckPolicyAsync("DropTable", true, "lead@example.com")).Ok);
            Assert.True((await service.SetCheckPolicyAsync("latejoin", true, "lead@example.com")).Ok);
            repo.Verify(r => r.SetCheckPolicyAsync("LateJoin", true, "lead@example.com"), Times.Once);
        }

        // ---------- Expiry + QC lead ----------

        [Fact]
        public async Task ExpireAndNotifyLead_EmailsLeadsThenMarksNotified()
        {
            var repo = new Mock<IDeductionReviewRepository>();
            repo.Setup(r => r.ExpireAsync(true)).ReturnsAsync(new[]
            {
                new DeductionReviewExpiredRowVM { Id = 4, InterviewId = 900, InterviewerEmail = "iv@example.com", DeductionPct = 5, AiCheck = "LateJoin",
                                                  RequestedAtUtc = NowUtc.AddDays(-8), DueAtUtc = NowUtc.AddDays(-1) }
            });
            var mail = new Mock<INotificationHelper>();
            mail.Setup(m => m.SendInterviewerCoachingEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("Mail sent successfully.");
            var settings = new DeductionReviewSettings { QcLeadEmails = "lead1@example.com; lead2@example.com" };

            var reported = await Service(repo, mail, settings).ExpireAndNotifyLeadAsync();

            Assert.Equal(1, reported);
            mail.Verify(m => m.SendInterviewerCoachingEmail(It.IsAny<string>(), null, null,
                It.Is<string>(s => s.Contains("1 deduction review request")), It.Is<string>(h => h.Contains("#900") && h.Contains("Late join"))), Times.Exactly(2));
            repo.Verify(r => r.MarkLeadNotifiedAsync(It.Is<IEnumerable<int>>(ids => ids.Single() == 4)), Times.Once);
        }

        [Fact]
        public async Task ExpireAndNotifyLead_WithoutLeadConfigured_DoesNotMarkNotified()
        {
            var repo = new Mock<IDeductionReviewRepository>();
            repo.Setup(r => r.ExpireAsync(true)).ReturnsAsync(new[] { new DeductionReviewExpiredRowVM { Id = 4, InterviewId = 900 } });

            var reported = await Service(repo).ExpireAndNotifyLeadAsync();

            Assert.Equal(0, reported);
            repo.Verify(r => r.MarkLeadNotifiedAsync(It.IsAny<IEnumerable<int>>()), Times.Never);
        }

        [Fact]
        public void LeadEmail_EncodesValues()
        {
            var (_, html) = DeductionReviewService.BuildLeadEmail(new[]
            {
                new DeductionReviewExpiredRowVM { Id = 1, InterviewId = 7, InterviewerEmail = "<script>@x.com", IrfName = "A&B", AiCheck = "Other" }
            }, Rules);

            Assert.DoesNotContain("<script>", html);
            Assert.Contains("A&amp;B", html);
        }

        // ---------- My Quality: review state per deduction ----------

        private static MyQualityDeductionVM Ded(int pct) => new MyQualityDeductionVM { InterviewId = 1, Percent = pct };

        private static MyQualityDeductionRowVM Row(int approval = 1, DateTime? appliedAt = null, string status = null) => new MyQualityDeductionRowVM
        {
            InterviewId = 1,
            ApprovalState = approval,
            AppliedAt = appliedAt ?? NowUtc.AddMinutes(330).AddDays(-2),
            InterviewDateIst = NowUtc.AddDays(-3),
            ReviewStatus = status
        };

        [Fact]
        public void ReviewState_ShowsButtonWhenRulesAllow()
        {
            var vm = InterviewerMyQualityService.ApplyReviewState(Ded(5), Row(), NowUtc, Rules, 0);
            Assert.True(vm.CanRequestReview);
            Assert.Null(vm.ReviewBlockedReason);
        }

        [Fact]
        public void ReviewState_GreyReasons()
        {
            Assert.Equal("Below 3%", InterviewerMyQualityService.ApplyReviewState(Ded(2), Row(), NowUtc, Rules, 0).ReviewBlockedReason);
            Assert.Equal("Older than 7 days", InterviewerMyQualityService.ApplyReviewState(Ded(5),
                Row(appliedAt: NowUtc.AddMinutes(330).AddDays(-8)), NowUtc, Rules, 0).ReviewBlockedReason);
            Assert.Equal("Monthly limit reached", InterviewerMyQualityService.ApplyReviewState(Ded(5), Row(), NowUtc, Rules, 3).ReviewBlockedReason);

            var notApplied = InterviewerMyQualityService.ApplyReviewState(Ded(5), Row(approval: 0), NowUtc, Rules, 0);
            Assert.False(notApplied.CanRequestReview);
            Assert.Null(notApplied.ReviewBlockedReason);
        }

        [Fact]
        public void ReviewState_Outcomes()
        {
            var open = Row(status: "Open");
            open.ReviewDueAtUtc = new DateTime(2026, 10, 21, 6, 0, 0, DateTimeKind.Utc);
            Assert.Equal("Under review · reply by 21 Oct", InterviewerMyQualityService.ApplyReviewState(Ded(5), open, NowUtc, Rules, 0).ReviewLabel);

            var reduced = Row(status: "Reduced");
            reduced.ReviewNewPct = 2;
            reduced.ReviewOriginalPct = 5;
            var r = InterviewerMyQualityService.ApplyReviewState(Ded(2), reduced, NowUtc, Rules, 0);
            Assert.Equal("Reviewed · reduced to 2%", r.ReviewLabel);
            Assert.Equal(5, r.Percent);

            Assert.Equal("Reviewed · refunded", InterviewerMyQualityService.ApplyReviewState(Ded(0), Row(status: "Refunded"), NowUtc, Rules, 0).ReviewLabel);
            Assert.Equal("Reviewed · deduction stands", InterviewerMyQualityService.ApplyReviewState(Ded(5), Row(status: "Stands"), NowUtc, Rules, 0).ReviewLabel);
            Assert.Equal("Reviewed · deduction stands", InterviewerMyQualityService.ApplyReviewState(Ded(5), Row(status: "Expired"), NowUtc, Rules, 0).ReviewLabel);
            Assert.False(InterviewerMyQualityService.ApplyReviewState(Ded(5), Row(status: "Stands"), NowUtc, Rules, 0).CanRequestReview);
        }

        [Fact]
        public void Build_KeepsRefundedDeductionsVisible()
        {
            var vm = InterviewerMyQualityService.Build(new InterviewerMyQualityDataVM
            {
                Deductions = new[]
                {
                    new MyQualityDeductionRowVM { InterviewId = 1, InterviewDateIst = new DateTime(2026, 10, 10), PaymentDeduction = 0,
                                                  ApprovalState = 1, ReviewStatus = "Refunded", ReviewOriginalPct = 5 },
                    new MyQualityDeductionRowVM { InterviewId = 2, InterviewDateIst = new DateTime(2026, 10, 9), PaymentDeduction = 0, ApprovalState = 1 }
                }
            }, NowUtc, Rules);

            var d = Assert.Single(vm.Deductions);
            Assert.Equal("Reviewed · refunded", d.ReviewLabel);
            Assert.Equal(5, d.Percent);
        }

        [Fact]
        public void AiCheckKeys_MatchTheSqlFunction()
        {
            var sqlKeys = new[] { "LateJoin", "ShortComment", "Duration", "Questions", "Decision", "Rubric", "StatusUpdate", "CameraOff",
                                  "Proctoring", "CandidateInteraction", "Communication", "Infrastructure", "HandsOn", "Conduct", "Other" };
            Assert.Equal(sqlKeys.OrderBy(k => k), DeductionReviewConstants.AiChecks.Keys.OrderBy(k => k));
        }
    }
}
