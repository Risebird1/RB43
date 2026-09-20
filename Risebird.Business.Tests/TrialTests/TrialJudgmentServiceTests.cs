using Microsoft.Extensions.Logging;
using Moq;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.TrialTests
{
    public sealed class TrialJudgmentFixture
    {
        public Mock<ITrialJudgmentRepository> Repository { get; } = new(MockBehavior.Loose);
        public Mock<IRiskCategoryService> RiskCategoryService { get; } = new(MockBehavior.Loose);
        public Mock<ILogger<TrialJudgmentService>> Logger { get; } = new(MockBehavior.Loose);

        public int InsertedTrialFlagId { get; set; } = 501;
        public bool InsertSucceeds { get; set; } = true;

        public TrialJudgmentService CreateSut()
        {
            RiskCategoryService.Setup(x => x.AddUpdateRiskCategory(It.IsAny<int>())).ReturnsAsync(true);
            RiskCategoryService.Setup(x => x.GetRiskCategoryByInterviewId(It.IsAny<int>()))
                .ReturnsAsync((RiskCategory)null);
            Repository.Setup(x => x.InsertTrialFlagAsync(
                    It.IsAny<TrialJudgmentResultVm>(), It.IsAny<TrialJudgmentContextVm>(), It.IsAny<int>()))
                .ReturnsAsync(() => (InsertSucceeds, InsertedTrialFlagId));
            Repository.Setup(x => x.GetBackfillCandidatesAsync())
                .ReturnsAsync((IReadOnlyList<TrialFlagBackfillCandidateVm>)new List<TrialFlagBackfillCandidateVm>());

            return new TrialJudgmentService(Repository.Object, RiskCategoryService.Object, Logger.Object);
        }

        /// <summary>A context that passes every eligibility gate and every quality factor.</summary>
        public static TrialJudgmentContextVm HappyContext() => new()
        {
            IsEligible = true,
            IsFirstCompletedInterview = true,
            AlreadyFlagged = false,
            ChannelId = 1,
            InterviewerId = 2,
            IrfId = 3,
            TriggeringInterviewDate = new DateTime(2026, 09, 01),
            TranscriptQualityScore = 9m,
            ReportQualityScore = 90m,
            CandidateRating = 4m,
            JoinTimeMinutes = 1m,
            InterviewDurationMinutes = 55m,
            RequiredDurationMinutes = 60
        };

        public static QcInterviewAIMergeAssistantResponse CleanMerge() => new()
        {
            interviewer_suspicious = "No",
            RedoInterview = false
        };
    }

    public class TrialJudgmentServiceEvaluateFactorsTests
    {
        [Fact]
        public void All_factors_passing_yields_a_good_trial()
        {
            var judgment = TrialJudgmentService.EvaluateFactors(
                TrialJudgmentFixture.HappyContext(), TrialJudgmentFixture.CleanMerge());

            Assert.Equal(TrialFlagConstants.VerdictGoodTrial, judgment.OverallVerdict);
            Assert.True(judgment.TranscriptQualityPass);
            Assert.True(judgment.ReportQualityPass);
            Assert.True(judgment.DecisionQualityPass);
            Assert.True(judgment.CandidateRatingPass);
            Assert.True(judgment.JoinTimePass);
            Assert.True(judgment.InterviewDurationPass);
        }

        [Fact]
        public void Every_missing_score_is_treated_as_a_pass()
        {
            var context = new TrialJudgmentContextVm();

            var judgment = TrialJudgmentService.EvaluateFactors(context, TrialJudgmentFixture.CleanMerge());

            Assert.Equal(TrialFlagConstants.VerdictGoodTrial, judgment.OverallVerdict);
            Assert.True(judgment.TranscriptQualityPass);
            Assert.True(judgment.ReportQualityPass);
            Assert.True(judgment.CandidateRatingPass);
            Assert.True(judgment.JoinTimePass);
            Assert.True(judgment.InterviewDurationPass);
        }

        [Fact]
        public void A_null_merge_result_is_treated_as_a_clean_decision()
        {
            var judgment = TrialJudgmentService.EvaluateFactors(TrialJudgmentFixture.HappyContext(), null);

            Assert.True(judgment.DecisionQualityPass);
            Assert.Equal(TrialFlagConstants.VerdictGoodTrial, judgment.OverallVerdict);
        }

        // ── Transcript quality ────────────────────────────────────────────────

        [Theory]
        [InlineData(4, true)]     // exactly the minimum passes
        [InlineData(4.01, true)]
        [InlineData(10, true)]
        [InlineData(3.99, false)]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        public void Transcript_score_passes_at_or_above_the_minimum(double score, bool expectedPass)
        {
            var context = TrialJudgmentFixture.HappyContext();
            context.TranscriptQualityScore = (decimal)score;

            var judgment = TrialJudgmentService.EvaluateFactors(context, TrialJudgmentFixture.CleanMerge());

            Assert.Equal(expectedPass, judgment.TranscriptQualityPass);
            Assert.Equal(
                expectedPass ? TrialFlagConstants.VerdictGoodTrial : TrialFlagConstants.VerdictBadTrial,
                judgment.OverallVerdict);
        }

        // ── Report quality ────────────────────────────────────────────────────

        [Theory]
        [InlineData(60, true)]
        [InlineData(100, true)]
        [InlineData(59.99, false)]
        [InlineData(0, false)]
        public void Report_score_passes_at_or_above_the_minimum(double score, bool expectedPass)
        {
            var context = TrialJudgmentFixture.HappyContext();
            context.ReportQualityScore = (decimal)score;

            var judgment = TrialJudgmentService.EvaluateFactors(context, TrialJudgmentFixture.CleanMerge());

            Assert.Equal(expectedPass, judgment.ReportQualityPass);
        }

        // ── Candidate rating ──────────────────────────────────────────────────

        [Theory]
        [InlineData(3, true)]
        [InlineData(5, true)]
        [InlineData(2.99, false)]
        [InlineData(0, false)]
        public void Candidate_rating_passes_at_or_above_the_minimum(double rating, bool expectedPass)
        {
            var context = TrialJudgmentFixture.HappyContext();
            context.CandidateRating = (decimal)rating;

            var judgment = TrialJudgmentService.EvaluateFactors(context, TrialJudgmentFixture.CleanMerge());

            Assert.Equal(expectedPass, judgment.CandidateRatingPass);
        }

        // ── Join time ─────────────────────────────────────────────────────────

        [Theory]
        [InlineData(0, true)]
        [InlineData(5, true)]      // exactly the ceiling passes
        [InlineData(5.01, false)]
        [InlineData(60, false)]
        [InlineData(-3, true)]     // joined early
        public void Join_time_passes_at_or_below_the_ceiling(double minutes, bool expectedPass)
        {
            var context = TrialJudgmentFixture.HappyContext();
            context.JoinTimeMinutes = (decimal)minutes;

            var judgment = TrialJudgmentService.EvaluateFactors(context, TrialJudgmentFixture.CleanMerge());

            Assert.Equal(expectedPass, judgment.JoinTimePass);
        }

        // ── Duration ratio ────────────────────────────────────────────────────

        [Theory]
        [InlineData(60, 48, true)]    // exactly 80%
        [InlineData(60, 60, true)]
        [InlineData(60, 90, true)]    // ran long
        [InlineData(60, 47.99, false)]
        [InlineData(60, 0, false)]
        public void Duration_passes_when_it_reaches_eighty_percent_of_the_required_time(
            int requiredMinutes, double actualMinutes, bool expectedPass)
        {
            var context = TrialJudgmentFixture.HappyContext();
            context.RequiredDurationMinutes = requiredMinutes;
            context.InterviewDurationMinutes = (decimal)actualMinutes;

            var judgment = TrialJudgmentService.EvaluateFactors(context, TrialJudgmentFixture.CleanMerge());

            Assert.Equal(expectedPass, judgment.InterviewDurationPass);
        }

        [Fact]
        public void Duration_is_not_checked_when_no_required_duration_is_configured()
        {
            var context = TrialJudgmentFixture.HappyContext();
            context.RequiredDurationMinutes = null;
            context.InterviewDurationMinutes = 1m;

            Assert.True(TrialJudgmentService
                .EvaluateFactors(context, TrialJudgmentFixture.CleanMerge()).InterviewDurationPass);
        }

        [Fact]
        public void Duration_is_not_checked_when_the_required_duration_is_zero()
        {
            var context = TrialJudgmentFixture.HappyContext();
            context.RequiredDurationMinutes = 0;
            context.InterviewDurationMinutes = 1m;

            Assert.True(TrialJudgmentService
                .EvaluateFactors(context, TrialJudgmentFixture.CleanMerge()).InterviewDurationPass);
        }

        [Fact]
        public void Duration_is_not_checked_when_the_actual_duration_is_unknown()
        {
            var context = TrialJudgmentFixture.HappyContext();
            context.RequiredDurationMinutes = 60;
            context.InterviewDurationMinutes = null;

            Assert.True(TrialJudgmentService
                .EvaluateFactors(context, TrialJudgmentFixture.CleanMerge()).InterviewDurationPass);
        }

        // ── Decision quality ──────────────────────────────────────────────────

        [Theory]
        [InlineData("Yes")]
        [InlineData("yes")]
        [InlineData("Y")]
        [InlineData("true")]
        public void A_suspicious_merge_verdict_fails_the_decision_factor(string suspicious)
        {
            var judgment = TrialJudgmentService.EvaluateFactors(
                TrialJudgmentFixture.HappyContext(),
                new QcInterviewAIMergeAssistantResponse { interviewer_suspicious = suspicious });

            Assert.False(judgment.DecisionQualityPass);
            Assert.Equal(TrialFlagConstants.VerdictBadTrial, judgment.OverallVerdict);
        }

        [Fact]
        public void A_redo_verdict_fails_the_decision_factor()
        {
            var judgment = TrialJudgmentService.EvaluateFactors(
                TrialJudgmentFixture.HappyContext(),
                new QcInterviewAIMergeAssistantResponse { RedoInterview = true });

            Assert.False(judgment.DecisionQualityPass);
            Assert.Equal(TrialFlagConstants.VerdictBadTrial, judgment.OverallVerdict);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("No", false)]
        [InlineData("", null)]
        [InlineData("pass", false)]
        [InlineData("conditional", null)]
        public void A_clean_merge_verdict_passes_the_decision_factor(string suspicious, bool? redo)
        {
            var judgment = TrialJudgmentService.EvaluateFactors(
                TrialJudgmentFixture.HappyContext(),
                new QcInterviewAIMergeAssistantResponse { interviewer_suspicious = suspicious, RedoInterview = redo });

            Assert.True(judgment.DecisionQualityPass);
        }

        // ── Combination ───────────────────────────────────────────────────────

        [Fact]
        public void Any_single_failing_factor_makes_the_whole_trial_bad()
        {
            var mutations = new List<Action<TrialJudgmentContextVm>>
            {
                c => c.TranscriptQualityScore = 1m,
                c => c.ReportQualityScore = 10m,
                c => c.CandidateRating = 1m,
                c => c.JoinTimeMinutes = 30m,
                c => { c.RequiredDurationMinutes = 60; c.InterviewDurationMinutes = 10m; }
            };

            foreach (var mutate in mutations)
            {
                var context = TrialJudgmentFixture.HappyContext();
                mutate(context);

                var judgment = TrialJudgmentService.EvaluateFactors(context, TrialJudgmentFixture.CleanMerge());

                Assert.Equal(TrialFlagConstants.VerdictBadTrial, judgment.OverallVerdict);
            }
        }

        [Fact]
        public void Several_failing_factors_are_all_reported()
        {
            var context = TrialJudgmentFixture.HappyContext();
            context.TranscriptQualityScore = 1m;
            context.ReportQualityScore = 10m;
            context.CandidateRating = 1m;
            context.JoinTimeMinutes = 30m;
            context.InterviewDurationMinutes = 10m;

            var judgment = TrialJudgmentService.EvaluateFactors(
                context, new QcInterviewAIMergeAssistantResponse { interviewer_suspicious = "Yes" });

            Assert.False(judgment.TranscriptQualityPass);
            Assert.False(judgment.ReportQualityPass);
            Assert.False(judgment.CandidateRatingPass);
            Assert.False(judgment.JoinTimePass);
            Assert.False(judgment.InterviewDurationPass);
            Assert.False(judgment.DecisionQualityPass);
            Assert.Equal(TrialFlagConstants.VerdictBadTrial, judgment.OverallVerdict);
        }

        [Fact]
        public void Evaluate_factors_never_sets_a_severity_on_its_own()
        {
            var judgment = TrialJudgmentService.EvaluateFactors(
                TrialJudgmentFixture.HappyContext(), TrialJudgmentFixture.CleanMerge());

            Assert.Null(judgment.Severity);
            Assert.Null(judgment.SkipReason);
            Assert.False(judgment.Inserted);
        }
    }

    public class TrialJudgmentServiceApplyRiskVerdictAndSeverityTests
    {
        private static TrialJudgmentResultVm Judgment(string verdict) =>
            new() { OverallVerdict = verdict };

        [Fact]
        public void A_live_risk_row_forces_a_bad_trial_and_adopts_its_severity()
        {
            var judgment = Judgment(TrialFlagConstants.VerdictGoodTrial);

            TrialJudgmentService.ApplyRiskVerdictAndSeverity(
                judgment, new RiskCategory { Severity = TrialFlagConstants.SeverityP0 });

            Assert.Equal(TrialFlagConstants.VerdictBadTrial, judgment.OverallVerdict);
            Assert.Equal(TrialFlagConstants.SeverityP0, judgment.Severity);
        }

        [Theory]
        [InlineData("P0")]
        [InlineData("P1")]
        [InlineData("P2")]
        [InlineData("P4")]
        public void Any_non_blank_severity_counts_as_live_risk(string severity)
        {
            var judgment = Judgment(TrialFlagConstants.VerdictGoodTrial);

            TrialJudgmentService.ApplyRiskVerdictAndSeverity(judgment, new RiskCategory { Severity = severity });

            Assert.Equal(TrialFlagConstants.VerdictBadTrial, judgment.OverallVerdict);
            Assert.Equal(severity, judgment.Severity);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void A_husk_risk_row_with_no_severity_is_not_live_risk(string severity)
        {
            var judgment = Judgment(TrialFlagConstants.VerdictGoodTrial);

            TrialJudgmentService.ApplyRiskVerdictAndSeverity(
                judgment, new RiskCategory { Severity = severity, Description = "narrative only" });

            Assert.Equal(TrialFlagConstants.VerdictGoodTrial, judgment.OverallVerdict);
            Assert.Equal(TrialFlagConstants.SeverityClean, judgment.Severity);
        }

        [Fact]
        public void No_risk_row_and_all_factors_passing_yields_the_clean_severity()
        {
            var judgment = Judgment(TrialFlagConstants.VerdictGoodTrial);

            TrialJudgmentService.ApplyRiskVerdictAndSeverity(judgment, null);

            Assert.Equal(TrialFlagConstants.VerdictGoodTrial, judgment.OverallVerdict);
            Assert.Equal(TrialFlagConstants.SeverityClean, judgment.Severity);
        }

        [Fact]
        public void No_risk_row_but_a_failed_factor_yields_the_p4_severity()
        {
            var judgment = Judgment(TrialFlagConstants.VerdictBadTrial);

            TrialJudgmentService.ApplyRiskVerdictAndSeverity(judgment, null);

            Assert.Equal(TrialFlagConstants.VerdictBadTrial, judgment.OverallVerdict);
            Assert.Equal(TrialFlagConstants.SeverityP4, judgment.Severity);
        }

        [Fact]
        public void Verdict_matching_is_case_insensitive()
        {
            var judgment = Judgment(TrialFlagConstants.VerdictGoodTrial.ToUpperInvariant());

            TrialJudgmentService.ApplyRiskVerdictAndSeverity(judgment, null);

            Assert.Equal(TrialFlagConstants.VerdictGoodTrial, judgment.OverallVerdict);
            Assert.Equal(TrialFlagConstants.SeverityClean, judgment.Severity);
        }

        [Fact]
        public void A_null_verdict_is_treated_as_a_failed_factor()
        {
            var judgment = Judgment(null);

            TrialJudgmentService.ApplyRiskVerdictAndSeverity(judgment, null);

            Assert.Equal(TrialFlagConstants.VerdictBadTrial, judgment.OverallVerdict);
            Assert.Equal(TrialFlagConstants.SeverityP4, judgment.Severity);
        }

        [Fact]
        public void A_missing_severity_is_never_promoted_to_p0()
        {
            // Guards the documented rule: "Never null-coalesce missing severity to P0".
            var judgment = Judgment(TrialFlagConstants.VerdictBadTrial);

            TrialJudgmentService.ApplyRiskVerdictAndSeverity(judgment, new RiskCategory { Severity = null });

            Assert.NotEqual(TrialFlagConstants.SeverityP0, judgment.Severity);
            Assert.Equal(TrialFlagConstants.SeverityP4, judgment.Severity);
        }

        [Fact]
        public void Severity_is_always_populated_afterwards()
        {
            foreach (var verdict in new[] { TrialFlagConstants.VerdictGoodTrial, TrialFlagConstants.VerdictBadTrial, null })
                foreach (var risk in new[] { null, new RiskCategory(), new RiskCategory { Severity = "P2" } })
                {
                    var judgment = Judgment(verdict);
                    TrialJudgmentService.ApplyRiskVerdictAndSeverity(judgment, risk);
                    Assert.False(string.IsNullOrWhiteSpace(judgment.Severity));
                }
        }
    }

    public class TrialJudgmentServiceTryEvaluateAndFlagTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Skips_an_invalid_interview_id_without_touching_the_repository(int interviewId)
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();

            var result = await sut.TryEvaluateAndFlagTrialAsync(interviewId, 0, TrialJudgmentFixture.CleanMerge());

            Assert.Equal("Invalid interview id.", result.SkipReason);
            Assert.False(result.Inserted);
            fixture.Repository.Verify(x => x.GetContextAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Skips_when_no_context_comes_back()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetContextAsync(1)).ReturnsAsync((TrialJudgmentContextVm)null);

            var result = await sut.TryEvaluateAndFlagTrialAsync(1, 0, TrialJudgmentFixture.CleanMerge());

            Assert.Equal("No trial judgment context returned.", result.SkipReason);
        }

        [Fact]
        public async Task Skips_when_this_is_not_the_first_completed_interview()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            var context = TrialJudgmentFixture.HappyContext();
            context.IsFirstCompletedInterview = false;
            fixture.Repository.Setup(x => x.GetContextAsync(1)).ReturnsAsync(context);

            var result = await sut.TryEvaluateAndFlagTrialAsync(1, 0, TrialJudgmentFixture.CleanMerge());

            Assert.Contains("Not the first completed interview", result.SkipReason);
        }

        [Fact]
        public async Task Skips_when_a_trial_flag_already_exists()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            var context = TrialJudgmentFixture.HappyContext();
            context.AlreadyFlagged = true;
            fixture.Repository.Setup(x => x.GetContextAsync(1)).ReturnsAsync(context);

            var result = await sut.TryEvaluateAndFlagTrialAsync(1, 0, TrialJudgmentFixture.CleanMerge());

            Assert.Contains("already exists", result.SkipReason);
        }

        [Theory]
        [InlineData(0, 3, 2)]
        [InlineData(1, 0, 2)]
        [InlineData(1, 3, 0)]
        [InlineData(-1, 3, 2)]
        public async Task Skips_when_the_interviewer_irf_mapping_cannot_be_resolved(
            int channelId, int irfId, int interviewerId)
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            var context = TrialJudgmentFixture.HappyContext();
            context.ChannelId = channelId;
            context.IrfId = irfId;
            context.InterviewerId = interviewerId;
            fixture.Repository.Setup(x => x.GetContextAsync(1)).ReturnsAsync(context);

            var result = await sut.TryEvaluateAndFlagTrialAsync(1, 0, TrialJudgmentFixture.CleanMerge());

            Assert.Contains("Could not resolve TransInterviewer", result.SkipReason);
        }

        [Fact]
        public async Task Skips_an_ineligible_interview()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            var context = TrialJudgmentFixture.HappyContext();
            context.IsEligible = false;
            fixture.Repository.Setup(x => x.GetContextAsync(1)).ReturnsAsync(context);

            var result = await sut.TryEvaluateAndFlagTrialAsync(1, 0, TrialJudgmentFixture.CleanMerge());

            Assert.Contains("not eligible", result.SkipReason);
        }

        [Fact]
        public async Task Skip_paths_never_write_a_trial_flag_or_refresh_risk()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetContextAsync(1)).ReturnsAsync((TrialJudgmentContextVm)null);

            await sut.TryEvaluateAndFlagTrialAsync(1, 0, TrialJudgmentFixture.CleanMerge());

            fixture.RiskCategoryService.Verify(x => x.AddUpdateRiskCategory(It.IsAny<int>()), Times.Never);
            fixture.Repository.Verify(
                x => x.InsertTrialFlagAsync(It.IsAny<TrialJudgmentResultVm>(),
                    It.IsAny<TrialJudgmentContextVm>(), It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task Refreshes_risk_before_reading_it()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            var order = new List<string>();
            fixture.Repository.Setup(x => x.GetContextAsync(1)).ReturnsAsync(TrialJudgmentFixture.HappyContext());
            fixture.RiskCategoryService.Setup(x => x.AddUpdateRiskCategory(1))
                .ReturnsAsync(() => { order.Add("refresh"); return true; });
            fixture.RiskCategoryService.Setup(x => x.GetRiskCategoryByInterviewId(1))
                .ReturnsAsync(() => { order.Add("read"); return (RiskCategory)null; });

            await sut.TryEvaluateAndFlagTrialAsync(1, 0, TrialJudgmentFixture.CleanMerge());

            Assert.Equal(new[] { "refresh", "read" }, order);
        }

        [Fact]
        public async Task Inserts_a_good_trial_flag_on_the_happy_path()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetContextAsync(1)).ReturnsAsync(TrialJudgmentFixture.HappyContext());

            var result = await sut.TryEvaluateAndFlagTrialAsync(1, 3, TrialJudgmentFixture.CleanMerge());

            Assert.True(result.Inserted);
            Assert.Equal(501, result.TrialFlagId);
            Assert.Equal(TrialFlagConstants.VerdictGoodTrial, result.OverallVerdict);
            Assert.Equal(TrialFlagConstants.SeverityClean, result.Severity);
            Assert.Null(result.SkipReason);
        }

        [Fact]
        public async Task A_live_risk_row_downgrades_the_flag_to_a_bad_trial()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetContextAsync(1)).ReturnsAsync(TrialJudgmentFixture.HappyContext());
            fixture.RiskCategoryService.Setup(x => x.GetRiskCategoryByInterviewId(1))
                .ReturnsAsync(new RiskCategory { Severity = "P1" });

            var result = await sut.TryEvaluateAndFlagTrialAsync(1, 3, TrialJudgmentFixture.CleanMerge());

            Assert.Equal(TrialFlagConstants.VerdictBadTrial, result.OverallVerdict);
            Assert.Equal("P1", result.Severity);
        }

        [Fact]
        public async Task An_irf_id_mismatch_is_logged_but_does_not_stop_the_insert()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetContextAsync(1)).ReturnsAsync(TrialJudgmentFixture.HappyContext());

            var result = await sut.TryEvaluateAndFlagTrialAsync(1, irfId: 999, TrialJudgmentFixture.CleanMerge());

            Assert.True(result.Inserted);
        }

        [Fact]
        public async Task A_zero_irf_id_skips_the_mismatch_check_entirely()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetContextAsync(1)).ReturnsAsync(TrialJudgmentFixture.HappyContext());

            var result = await sut.TryEvaluateAndFlagTrialAsync(1, irfId: 0, TrialJudgmentFixture.CleanMerge());

            Assert.True(result.Inserted);
        }

        [Fact]
        public async Task A_failed_insert_is_reported_with_a_skip_reason()
        {
            var fixture = new TrialJudgmentFixture { InsertSucceeds = false, InsertedTrialFlagId = 0 };
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetContextAsync(1)).ReturnsAsync(TrialJudgmentFixture.HappyContext());

            var result = await sut.TryEvaluateAndFlagTrialAsync(1, 3, TrialJudgmentFixture.CleanMerge());

            Assert.False(result.Inserted);
            Assert.Null(result.TrialFlagId);
            Assert.Contains("did not create a row", result.SkipReason);
        }

        [Fact]
        public async Task A_non_positive_trial_flag_id_is_normalised_to_null()
        {
            var fixture = new TrialJudgmentFixture { InsertSucceeds = true, InsertedTrialFlagId = 0 };
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetContextAsync(1)).ReturnsAsync(TrialJudgmentFixture.HappyContext());

            var result = await sut.TryEvaluateAndFlagTrialAsync(1, 3, TrialJudgmentFixture.CleanMerge());

            Assert.True(result.Inserted);
            Assert.Null(result.TrialFlagId);
        }

        [Fact]
        public async Task Honours_a_cancellation_request_before_doing_any_work()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                sut.TryEvaluateAndFlagTrialAsync(1, 3, TrialJudgmentFixture.CleanMerge(), cts.Token));

            fixture.Repository.Verify(x => x.GetContextAsync(It.IsAny<int>()), Times.Never);
        }
    }

    public class TrialJudgmentServiceCompletedMergeTests
    {
        [Fact]
        public async Task Skips_when_no_stored_merge_result_exists()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetStoredMergeResultAsync(1))
                .ReturnsAsync(((QcInterviewAIMergeAssistantResponse, int)?)null);

            var result = await sut.TryEvaluateAndFlagTrialForCompletedMergeAsync(1);

            Assert.Contains("QC merge not completed", result.SkipReason);
            fixture.Repository.Verify(x => x.GetContextAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Forwards_the_stored_merge_result_and_irf_id()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetStoredMergeResultAsync(1))
                .ReturnsAsync((TrialJudgmentFixture.CleanMerge(), 3));
            fixture.Repository.Setup(x => x.GetContextAsync(1)).ReturnsAsync(TrialJudgmentFixture.HappyContext());

            var result = await sut.TryEvaluateAndFlagTrialForCompletedMergeAsync(1);

            Assert.True(result.Inserted);
            Assert.Equal(TrialFlagConstants.VerdictGoodTrial, result.OverallVerdict);
        }

        [Fact]
        public async Task A_stored_suspicious_verdict_produces_a_bad_trial()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetStoredMergeResultAsync(1))
                .ReturnsAsync((new QcInterviewAIMergeAssistantResponse { interviewer_suspicious = "Yes" }, 3));
            fixture.Repository.Setup(x => x.GetContextAsync(1)).ReturnsAsync(TrialJudgmentFixture.HappyContext());

            var result = await sut.TryEvaluateAndFlagTrialForCompletedMergeAsync(1);

            Assert.Equal(TrialFlagConstants.VerdictBadTrial, result.OverallVerdict);
            Assert.Equal(TrialFlagConstants.SeverityP4, result.Severity);
        }

        [Fact]
        public async Task Honours_a_cancellation_request()
        {
            var sut = new TrialJudgmentFixture().CreateSut();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                sut.TryEvaluateAndFlagTrialForCompletedMergeAsync(1, cts.Token));
        }
    }

    public class TrialJudgmentServiceBackfillTests
    {
        [Fact]
        public async Task Reports_no_eligible_pairs_with_a_diagnostic_hint()
        {
            var sut = new TrialJudgmentFixture().CreateSut();

            var result = await sut.BackfillMissingTrialFlagsAsync();

            Assert.Equal(0, result.Inserted);
            Assert.Equal(0, result.Skipped);
            Assert.Equal(0, result.Failed);
            Assert.Contains(result.Messages, m => m.Contains("No eligible pairs found"));
            Assert.Contains(result.Messages, m => m.Contains(TrialFlagConstants.BackfillActivityWindowDays.ToString()));
        }

        [Fact]
        public async Task A_candidate_load_failure_is_reported_as_a_single_failure()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetBackfillCandidatesAsync())
                .ThrowsAsync(new InvalidOperationException("sp missing"));

            var result = await sut.BackfillMissingTrialFlagsAsync();

            Assert.Equal(1, result.Failed);
            Assert.Contains(result.Messages, m => m.Contains("Could not load backfill candidates"));
            Assert.Contains(result.Messages, m => m.Contains("sp missing"));
        }

        [Fact]
        public async Task An_explicit_interview_id_bypasses_the_candidate_query()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetStoredMergeResultAsync(42))
                .ReturnsAsync((TrialJudgmentFixture.CleanMerge(), 3));
            fixture.Repository.Setup(x => x.GetContextAsync(42)).ReturnsAsync(TrialJudgmentFixture.HappyContext());

            var result = await sut.BackfillMissingTrialFlagsAsync(interviewId: 42);

            Assert.Equal(1, result.Inserted);
            fixture.Repository.Verify(x => x.GetBackfillCandidatesAsync(), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task A_non_positive_interview_id_falls_back_to_the_candidate_query(int interviewId)
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();

            await sut.BackfillMissingTrialFlagsAsync(interviewId);

            fixture.Repository.Verify(x => x.GetBackfillCandidatesAsync(), Times.Once);
        }

        [Fact]
        public async Task Counts_inserts_and_skips_separately()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetBackfillCandidatesAsync())
                .ReturnsAsync((IReadOnlyList<TrialFlagBackfillCandidateVm>)new List<TrialFlagBackfillCandidateVm>
                {
                    new() { InterviewId = 1, IrfId = 3 },
                    new() { InterviewId = 2, IrfId = 3 }
                });
            fixture.Repository.Setup(x => x.GetStoredMergeResultAsync(1))
                .ReturnsAsync((TrialJudgmentFixture.CleanMerge(), 3));
            fixture.Repository.Setup(x => x.GetContextAsync(1)).ReturnsAsync(TrialJudgmentFixture.HappyContext());
            fixture.Repository.Setup(x => x.GetStoredMergeResultAsync(2))
                .ReturnsAsync(((QcInterviewAIMergeAssistantResponse, int)?)null);

            var result = await sut.BackfillMissingTrialFlagsAsync();

            Assert.Equal(1, result.Inserted);
            Assert.Equal(1, result.Skipped);
            Assert.Equal(0, result.Failed);
        }

        [Fact]
        public async Task A_thrown_pair_is_counted_as_failed_without_aborting_the_batch()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetBackfillCandidatesAsync())
                .ReturnsAsync((IReadOnlyList<TrialFlagBackfillCandidateVm>)new List<TrialFlagBackfillCandidateVm>
                {
                    new() { InterviewId = 1, IrfId = 3 },
                    new() { InterviewId = 2, IrfId = 3 }
                });
            fixture.Repository.Setup(x => x.GetStoredMergeResultAsync(1))
                .ThrowsAsync(new InvalidOperationException("boom"));
            fixture.Repository.Setup(x => x.GetStoredMergeResultAsync(2))
                .ReturnsAsync((TrialJudgmentFixture.CleanMerge(), 3));
            fixture.Repository.Setup(x => x.GetContextAsync(2)).ReturnsAsync(TrialJudgmentFixture.HappyContext());

            var result = await sut.BackfillMissingTrialFlagsAsync();

            Assert.Equal(1, result.Failed);
            Assert.Equal(1, result.Inserted);
            Assert.Contains(result.Messages, m => m.Contains("Failed pair interview=1"));
        }

        [Fact]
        public async Task Stops_after_the_configured_insert_batch_size()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            var candidates = Enumerable.Range(1, TrialFlagConstants.BackfillBatchSize + 10)
                .Select(i => new TrialFlagBackfillCandidateVm { InterviewId = i, IrfId = 3 })
                .ToList();
            fixture.Repository.Setup(x => x.GetBackfillCandidatesAsync())
                .ReturnsAsync((IReadOnlyList<TrialFlagBackfillCandidateVm>)candidates);
            fixture.Repository.Setup(x => x.GetStoredMergeResultAsync(It.IsAny<int>()))
                .ReturnsAsync((TrialJudgmentFixture.CleanMerge(), 3));
            fixture.Repository.Setup(x => x.GetContextAsync(It.IsAny<int>()))
                .ReturnsAsync(TrialJudgmentFixture.HappyContext);

            var result = await sut.BackfillMissingTrialFlagsAsync();

            Assert.Equal(TrialFlagConstants.BackfillBatchSize, result.Inserted);
        }

        [Fact]
        public async Task Summarises_the_top_skip_reasons_when_nothing_was_inserted()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            var candidates = Enumerable.Range(1, 4)
                .Select(i => new TrialFlagBackfillCandidateVm { InterviewId = i, IrfId = 3 })
                .ToList();
            fixture.Repository.Setup(x => x.GetBackfillCandidatesAsync())
                .ReturnsAsync((IReadOnlyList<TrialFlagBackfillCandidateVm>)candidates);
            fixture.Repository.Setup(x => x.GetStoredMergeResultAsync(It.IsAny<int>()))
                .ReturnsAsync(((QcInterviewAIMergeAssistantResponse, int)?)null);

            var result = await sut.BackfillMissingTrialFlagsAsync();

            Assert.Equal(0, result.Inserted);
            Assert.Equal(4, result.Skipped);
            Assert.Contains(result.Messages, m => m.StartsWith("Skip summary:"));
            Assert.Contains(result.Messages, m => m.Contains("4×"));
        }

        [Fact]
        public async Task No_skip_summary_is_added_when_something_was_inserted()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetBackfillCandidatesAsync())
                .ReturnsAsync((IReadOnlyList<TrialFlagBackfillCandidateVm>)new List<TrialFlagBackfillCandidateVm>
                {
                    new() { InterviewId = 1, IrfId = 3 },
                    new() { InterviewId = 2, IrfId = 3 }
                });
            fixture.Repository.Setup(x => x.GetStoredMergeResultAsync(1))
                .ReturnsAsync((TrialJudgmentFixture.CleanMerge(), 3));
            fixture.Repository.Setup(x => x.GetContextAsync(1)).ReturnsAsync(TrialJudgmentFixture.HappyContext());
            fixture.Repository.Setup(x => x.GetStoredMergeResultAsync(2))
                .ReturnsAsync(((QcInterviewAIMergeAssistantResponse, int)?)null);

            var result = await sut.BackfillMissingTrialFlagsAsync();

            Assert.DoesNotContain(result.Messages, m => m.StartsWith("Skip summary:"));
        }

        [Fact]
        public async Task Announces_how_many_pairs_it_will_try()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetBackfillCandidatesAsync())
                .ReturnsAsync((IReadOnlyList<TrialFlagBackfillCandidateVm>)new List<TrialFlagBackfillCandidateVm>
                {
                    new() { InterviewId = 1, IrfId = 3 }
                });
            fixture.Repository.Setup(x => x.GetStoredMergeResultAsync(It.IsAny<int>()))
                .ReturnsAsync(((QcInterviewAIMergeAssistantResponse, int)?)null);

            var result = await sut.BackfillMissingTrialFlagsAsync();

            Assert.Contains(result.Messages, m => m.Contains("Trying up to 1 active interviewer"));
        }

        [Fact]
        public async Task Honours_a_cancellation_request_mid_batch()
        {
            var fixture = new TrialJudgmentFixture();
            var sut = fixture.CreateSut();
            using var cts = new CancellationTokenSource();
            fixture.Repository.Setup(x => x.GetBackfillCandidatesAsync())
                .ReturnsAsync((IReadOnlyList<TrialFlagBackfillCandidateVm>)new List<TrialFlagBackfillCandidateVm>
                {
                    new() { InterviewId = 1, IrfId = 3 }
                });
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                sut.BackfillMissingTrialFlagsAsync(null, cts.Token));
        }
    }
}
