using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;

namespace Risebird.CommonHelper.Tests.EntitiesHelpers
{
    public class StatsDetailsPaymentDeductionHelperTests
    {
        [Fact]
        public void A_null_summary_yields_a_blank_non_clickable_cell()
        {
            var display = StatsDetailsPaymentDeductionHelper.ResolveDisplay(null);

            Assert.NotNull(display);
            Assert.Null(display.StatusLabel);
            Assert.Null(display.DeductionPercent);
            Assert.Empty(display.RedFlagTags);
            Assert.False(display.IsClickable);
        }

        [Fact]
        public void An_empty_summary_yields_a_blank_non_clickable_cell()
        {
            var display = StatsDetailsPaymentDeductionHelper.ResolveDisplay(
                new StatsDetailsPaymentDeductionSummary { InterviewId = 1 });

            Assert.Null(display.StatusLabel);
            Assert.False(display.IsClickable);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(null)]
        public void A_zero_or_missing_deduction_with_no_flags_stays_blank(int? deduction)
        {
            var display = StatsDetailsPaymentDeductionHelper.ResolveDisplay(
                new StatsDetailsPaymentDeductionSummary { PaymentDeduction = deduction });

            Assert.Null(display.StatusLabel);
            Assert.False(display.IsClickable);
        }

        [Fact]
        public void A_positive_deduction_shows_the_deducted_status_and_the_percent()
        {
            var display = StatsDetailsPaymentDeductionHelper.ResolveDisplay(
                new StatsDetailsPaymentDeductionSummary { PaymentDeduction = 25 });

            Assert.Equal(StatsDetailsPaymentDeductionHelper.StatusDeducted, display.StatusLabel);
            Assert.Equal(25, display.DeductionPercent);
            Assert.True(display.IsClickable);
        }

        [Fact]
        public void Flags_without_a_deduction_show_the_flags_status_and_no_percent()
        {
            var display = StatsDetailsPaymentDeductionHelper.ResolveDisplay(
                new StatsDetailsPaymentDeductionSummary { WeakInfrastructure = "camera off" });

            Assert.Equal(StatsDetailsPaymentDeductionHelper.StatusFlags, display.StatusLabel);
            Assert.Null(display.DeductionPercent);
            Assert.True(display.IsClickable);
            Assert.Equal(new[] { "camera off" }, display.RedFlagTags);
        }

        [Fact]
        public void A_compliance_count_alone_is_enough_to_make_the_cell_clickable()
        {
            var display = StatsDetailsPaymentDeductionHelper.ResolveDisplay(
                new StatsDetailsPaymentDeductionSummary { PaymentComplianceCount = 2 });

            Assert.Equal(StatsDetailsPaymentDeductionHelper.StatusFlags, display.StatusLabel);
            Assert.True(display.IsClickable);
            Assert.Empty(display.RedFlagTags);
        }

        [Fact]
        public void A_zero_compliance_count_is_not_a_flag()
        {
            var display = StatsDetailsPaymentDeductionHelper.ResolveDisplay(
                new StatsDetailsPaymentDeductionSummary { PaymentComplianceCount = 0 });

            Assert.Null(display.StatusLabel);
            Assert.False(display.IsClickable);
        }

        [Fact]
        public void Deduction_wins_over_flags_for_the_status_label()
        {
            var display = StatsDetailsPaymentDeductionHelper.ResolveDisplay(
                new StatsDetailsPaymentDeductionSummary
                {
                    PaymentDeduction = 10,
                    UnethicalPractices = "proxy suspected"
                });

            Assert.Equal(StatsDetailsPaymentDeductionHelper.StatusDeducted, display.StatusLabel);
            Assert.Equal(10, display.DeductionPercent);
            Assert.Contains("proxy suspected", display.RedFlagTags);
        }

        [Fact]
        public void Tags_are_split_on_commas_trimmed_deduplicated_and_sorted()
        {
            var display = StatsDetailsPaymentDeductionHelper.ResolveDisplay(
                new StatsDetailsPaymentDeductionSummary
                {
                    NonAIreasoning = " zebra , alpha ,alpha",
                    Riskcategory = "ALPHA, middle"
                });

            Assert.Equal(new[] { "alpha", "middle", "zebra" }, display.RedFlagTags);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("no")]
        [InlineData("n")]
        [InlineData("false")]
        [InlineData("na")]
        [InlineData("n/a")]
        [InlineData("none")]
        [InlineData("NONE")]
        [InlineData("  No  ")]
        public void Noise_tokens_never_become_tags(string value)
        {
            var display = StatsDetailsPaymentDeductionHelper.ResolveDisplay(
                new StatsDetailsPaymentDeductionSummary { WeakInfrastructure = value });

            Assert.Empty(display.RedFlagTags);
            Assert.Null(display.StatusLabel);
        }

        [Fact]
        public void Collects_tags_from_every_reason_column()
        {
            var summary = new StatsDetailsPaymentDeductionSummary
            {
                NonAIreasoning = "c01",
                Riskcategory = "c02",
                Inaccuratedecisionmaking = "c03",
                TimeManagement = "c04",
                QuestionsRelevance = "c05",
                ScoreRubricIgnorance = "c06",
                PoorFeedbackComments = "c07",
                CandidateInteractionQuality = "c08",
                LimitedHandoncoding = "c09",
                WrongStatusUpdates = "c10",
                WeakInfrastructure = "c11",
                ProctoringIgnorance = "c12",
                UnethicalPractices = "c13",
                PoorCommunicationSkills = "c14",
                InterviewerVideo = "c15",
                AttendanceJoinCompliance = "c16",
                AIContradictionScore = "c17"
            };

            var display = StatsDetailsPaymentDeductionHelper.ResolveDisplay(summary);

            Assert.Equal(17, display.RedFlagTags.Count);
        }

        [Fact]
        public void Status_label_constants_are_the_expected_strings()
        {
            Assert.Equal("Deducted", StatsDetailsPaymentDeductionHelper.StatusDeducted);
            Assert.Equal("Flags", StatsDetailsPaymentDeductionHelper.StatusFlags);
        }
    }

    public class StatsDetailsQcVerdictHelperTests
    {
        [Fact]
        public void A_null_evaluation_renders_blank() =>
            Assert.Null(StatsDetailsQcVerdictHelper.ResolveDisplay(null));

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        public void An_unfinished_qc_row_renders_pending(bool? qcDone)
        {
            var display = StatsDetailsQcVerdictHelper.ResolveDisplay(
                new QcInterviewAIevaluation { QCDone = qcDone });

            Assert.Equal(StatsDetailsQcVerdictHelper.DisplayPending, display);
        }

        [Fact]
        public void A_finished_row_with_no_verdict_renders_blank()
        {
            var display = StatsDetailsQcVerdictHelper.ResolveDisplay(
                new QcInterviewAIevaluation { QCDone = true });

            Assert.Null(display);
        }

        [Theory]
        [InlineData("Yes")]
        [InlineData("Y")]
        [InlineData("true")]
        public void A_suspicious_column_value_renders_suspicious(string suspicious)
        {
            var display = StatsDetailsQcVerdictHelper.ResolveDisplay(
                new QcInterviewAIevaluation { QCDone = true, AIMergeInterviewerSuspicious = suspicious });

            Assert.Equal(StatsDetailsQcVerdictHelper.DisplaySuspicious, display);
        }

        [Fact]
        public void A_redo_column_value_renders_redo()
        {
            var display = StatsDetailsQcVerdictHelper.ResolveDisplay(
                new QcInterviewAIevaluation { QCDone = true, AIMergeRedoInterview = true });

            Assert.Equal(StatsDetailsQcVerdictHelper.DisplayRedo, display);
        }

        [Fact]
        public void Suspicious_wins_over_redo_when_both_are_set()
        {
            var display = StatsDetailsQcVerdictHelper.ResolveDisplay(new QcInterviewAIevaluation
            {
                QCDone = true,
                AIMergeInterviewerSuspicious = "Yes",
                AIMergeRedoInterview = true
            });

            Assert.Equal(StatsDetailsQcVerdictHelper.DisplaySuspicious, display);
        }

        [Fact]
        public void Falls_back_to_the_merged_json_when_the_columns_are_empty()
        {
            var display = StatsDetailsQcVerdictHelper.ResolveDisplay(new QcInterviewAIevaluation
            {
                QCDone = true,
                MergedAIQCDetails = """{"interviewer_suspicious":"Yes"}"""
            });

            Assert.Equal(StatsDetailsQcVerdictHelper.DisplaySuspicious, display);
        }

        [Fact]
        public void Merged_json_can_promote_a_no_column_to_suspicious()
        {
            // The column says "No" but the stored merge JSON says "Yes" — the stronger
            // signal must win so a suspicious verdict is never silently dropped.
            var display = StatsDetailsQcVerdictHelper.ResolveDisplay(new QcInterviewAIevaluation
            {
                QCDone = true,
                AIMergeInterviewerSuspicious = "No",
                MergedAIQCDetails = """{"interviewer_suspicious":"Yes"}"""
            });

            Assert.Equal(StatsDetailsQcVerdictHelper.DisplaySuspicious, display);
        }

        [Fact]
        public void Merged_json_can_promote_a_missing_redo_flag()
        {
            var display = StatsDetailsQcVerdictHelper.ResolveDisplay(new QcInterviewAIevaluation
            {
                QCDone = true,
                AIMergeRedoInterview = false,
                MergedAIQCDetails = """{"RedoInterview":true}"""
            });

            Assert.Equal(StatsDetailsQcVerdictHelper.DisplayRedo, display);
        }

        [Fact]
        public void Merged_json_never_downgrades_an_affirmative_column()
        {
            var display = StatsDetailsQcVerdictHelper.ResolveDisplay(new QcInterviewAIevaluation
            {
                QCDone = true,
                AIMergeInterviewerSuspicious = "Yes",
                MergedAIQCDetails = """{"interviewer_suspicious":"No","RedoInterview":false}"""
            });

            Assert.Equal(StatsDetailsQcVerdictHelper.DisplaySuspicious, display);
        }

        [Theory]
        [InlineData("not json")]
        [InlineData("{")]
        [InlineData("[]")]
        public void Unparseable_merged_json_is_ignored_rather_than_throwing(string json)
        {
            var display = StatsDetailsQcVerdictHelper.ResolveDisplay(new QcInterviewAIevaluation
            {
                QCDone = true,
                AIMergeInterviewerSuspicious = "Yes",
                MergedAIQCDetails = json
            });

            Assert.Equal(StatsDetailsQcVerdictHelper.DisplaySuspicious, display);
        }

        [Fact]
        public void A_clean_merge_result_renders_blank()
        {
            var display = StatsDetailsQcVerdictHelper.ResolveDisplay(new QcInterviewAIevaluation
            {
                QCDone = true,
                MergedAIQCDetails = """{"interviewer_suspicious":"No","RedoInterview":false,"qcmerge_verdict":"pass"}"""
            });

            Assert.Null(display);
        }

        [Theory]
        [InlineData("Suspicious", true)]
        [InlineData("Redo", true)]
        [InlineData("Pending", false)]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("suspicious", false)]   // case-sensitive by design
        public void Only_suspicious_and_redo_are_linkable(string display, bool expected) =>
            Assert.Equal(expected, StatsDetailsQcVerdictHelper.IsLinkable(display));

        [Fact]
        public void Display_constants_are_the_expected_strings()
        {
            Assert.Equal("Suspicious", StatsDetailsQcVerdictHelper.DisplaySuspicious);
            Assert.Equal("Redo", StatsDetailsQcVerdictHelper.DisplayRedo);
            Assert.Equal("Pending", StatsDetailsQcVerdictHelper.DisplayPending);
        }
    }
}
