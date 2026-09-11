using Risebird.Entities.ViewModel;

namespace Risebird.CommonHelper.Tests.EntitiesHelpers
{
    public class DmIvAutoQcAuditConstantsIsAffirmativeSuspiciousTests
    {
        [Theory]
        [InlineData("Yes")]
        [InlineData("yes")]
        [InlineData("YES")]
        [InlineData("yEs")]
        [InlineData("Y")]
        [InlineData("y")]
        [InlineData("True")]
        [InlineData("true")]
        [InlineData("TRUE")]
        [InlineData("  Yes  ")]
        [InlineData("\tY\n")]
        public void Recognises_every_affirmative_token(string value) =>
            Assert.True(DmIvAutoQcAuditConstants.IsAffirmativeSuspicious(value));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("No")]
        [InlineData("no")]
        [InlineData("N")]
        [InlineData("False")]
        [InlineData("false")]
        [InlineData("0")]
        [InlineData("1")]
        [InlineData("maybe")]
        [InlineData("pass")]
        [InlineData("conditional")]
        [InlineData("manual review")]
        [InlineData("Yes please")]
        [InlineData("Yess")]
        [InlineData("YY")]
        public void Rejects_everything_else(string value) =>
            Assert.False(DmIvAutoQcAuditConstants.IsAffirmativeSuspicious(value));

        [Fact]
        public void None_of_the_documented_non_triggering_outcomes_is_affirmative()
        {
            foreach (var outcome in DmIvAutoQcAuditConstants.NonTriggeringMergeOutcomes)
                Assert.False(DmIvAutoQcAuditConstants.IsAffirmativeSuspicious(outcome));
        }
    }

    public class DmIvAutoQcAuditConstantsResolveVerdictTests
    {
        [Theory]
        [InlineData("Yes", null)]
        [InlineData("Yes", true)]
        [InlineData("Yes", false)]
        [InlineData("Y", true)]
        [InlineData("true", false)]
        public void Suspicious_wins_over_redo(string suspicious, bool? redo) =>
            Assert.Equal(
                DmIvAutoQcAuditConstants.VerdictSuspicious,
                DmIvAutoQcAuditConstants.ResolveVerdict(suspicious, redo));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("No")]
        [InlineData("pass")]
        public void Redo_applies_when_suspicious_is_not_affirmative(string suspicious) =>
            Assert.Equal(
                DmIvAutoQcAuditConstants.VerdictRedo,
                DmIvAutoQcAuditConstants.ResolveVerdict(suspicious, true));

        [Theory]
        [InlineData(null, null)]
        [InlineData(null, false)]
        [InlineData("", null)]
        [InlineData("   ", false)]
        [InlineData("No", null)]
        [InlineData("No", false)]
        [InlineData("false", false)]
        [InlineData("pass", null)]
        [InlineData("conditional", false)]
        [InlineData("manual review", null)]
        public void Returns_null_when_nothing_triggers(string suspicious, bool? redo) =>
            Assert.Null(DmIvAutoQcAuditConstants.ResolveVerdict(suspicious, redo));

        [Fact]
        public void Verdict_values_are_the_lowercase_tokens_the_sql_layer_expects()
        {
            Assert.Equal("suspicious", DmIvAutoQcAuditConstants.VerdictSuspicious);
            Assert.Equal("redo", DmIvAutoQcAuditConstants.VerdictRedo);
        }
    }

    public class DmIvAutoQcAuditConstantsThresholdTests
    {
        [Fact]
        public void Audit_thresholds_match_the_stored_procedure_declarations()
        {
            Assert.Equal(3, DmIvAutoQcAuditConstants.IrfAuditMinVolume);
            Assert.Equal(0.50m, DmIvAutoQcAuditConstants.IrfAuditRemoveRatio);
            Assert.Equal(0.25m, DmIvAutoQcAuditConstants.IrfAuditHighSevRatioRem);
            Assert.Equal(0.25m, DmIvAutoQcAuditConstants.IrfAuditWatchRatio);
            Assert.Equal(0.25m, DmIvAutoQcAuditConstants.IrfAuditOverviewDefaultMinRiskRatio);
        }

        [Fact]
        public void Backfill_limits_are_positive_and_ordered()
        {
            Assert.True(DmIvAutoQcAuditConstants.RiskCategoryBackfillBatchSize > 0);
            Assert.True(DmIvAutoQcAuditConstants.RiskCategoryBackfillMaxBatches > 0);
        }

        [Fact]
        public void Mass_fire_guard_thresholds_are_sane()
        {
            Assert.InRange(DmIvAutoQcAuditConstants.MassFireThresholdPct, 1, 100);
            Assert.True(DmIvAutoQcAuditConstants.MassFireMinSample > 0);
        }

        [Fact]
        public void Remove_ratio_is_stricter_than_the_watch_ratio() =>
            Assert.True(DmIvAutoQcAuditConstants.IrfAuditRemoveRatio > DmIvAutoQcAuditConstants.IrfAuditWatchRatio);

        [Fact]
        public void Audit_tags_and_sources_are_distinct_non_empty_tokens()
        {
            var tokens = new[]
            {
                DmIvAutoQcAuditConstants.Tag,
                DmIvAutoQcAuditConstants.FlagTag,
                DmIvAutoQcAuditConstants.SourceForwardTrigger,
                DmIvAutoQcAuditConstants.SourceRetroactiveBackfill60Day,
                DmIvAutoQcAuditConstants.SourcePerIrfBackfill,
                DmIvAutoQcAuditConstants.SourceRiskAuditManual,
                DmIvAutoQcAuditConstants.RiskAuditTag
            };

            Assert.All(tokens, t => Assert.False(string.IsNullOrWhiteSpace(t)));
            Assert.Equal(tokens.Length, tokens.Distinct(StringComparer.Ordinal).Count());
        }
    }

    public class DmIvAutoQcAuditConstantsUrlTests
    {
        [Theory]
        [InlineData(1, "/Interview/QCMergedAIDetails?interviewId=1")]
        [InlineData(123456, "/Interview/QCMergedAIDetails?interviewId=123456")]
        [InlineData(0, "/Interview/QCMergedAIDetails?interviewId=0")]
        [InlineData(-5, "/Interview/QCMergedAIDetails?interviewId=-5")]
        public void Builds_the_qc_merge_details_link(int interviewId, string expected) =>
            Assert.Equal(expected, DmIvAutoQcAuditConstants.QcMergedAiDetailsUrl(interviewId));

        [Fact]
        public void Link_is_relative_so_it_works_behind_any_host() =>
            Assert.StartsWith("/", DmIvAutoQcAuditConstants.QcMergedAiDetailsUrl(1));
    }

    public class DmIvInactiveAuditEventTypesTests
    {
        [Fact]
        public void Event_types_are_distinct_non_empty_tokens()
        {
            var tokens = new[]
            {
                DmIvInactiveAuditEventTypes.RbVetFlippedToNo,
                DmIvInactiveAuditEventTypes.RbVetFlippedToYes,
                DmIvInactiveAuditEventTypes.ClientVetFlippedToNo,
                DmIvInactiveAuditEventTypes.ClientVetFlippedToYes,
                DmIvInactiveAuditEventTypes.AutoQcRemoval,
                DmIvInactiveAuditEventTypes.AutoQcFlag,
                DmIvInactiveAuditEventTypes.RiskAuditRemoval,
                DmIvInactiveAuditEventTypes.RiskAuditKept
            };

            Assert.All(tokens, t => Assert.False(string.IsNullOrWhiteSpace(t)));
            Assert.Equal(tokens.Length, tokens.Distinct(StringComparer.Ordinal).Count());
        }

        [Fact]
        public void Flag_and_removal_are_separate_event_types() =>
            Assert.NotEqual(DmIvInactiveAuditEventTypes.AutoQcFlag, DmIvInactiveAuditEventTypes.AutoQcRemoval);
    }
}
