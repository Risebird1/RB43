using Risebird.Business;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;

namespace Risebird.Business.Tests.TrialTests
{
    public class RiskCategoryMassFireGuardParseFiredCheckTypesTests
    {
        [Fact]
        public void Parses_a_single_check_with_a_severity_suffix() =>
            Assert.Equal(new[] { "CheckType" },
                RiskCategoryMassFireGuard.ParseFiredCheckTypes("CheckType:P0"));

        [Fact]
        public void Parses_several_comma_separated_checks() =>
            Assert.Equal(new[] { "CheckA", "CheckB", "CheckC" },
                RiskCategoryMassFireGuard.ParseFiredCheckTypes("CheckA:P0,CheckB:P2,CheckC:P4"));

        [Fact]
        public void Accepts_semicolons_as_separators_too() =>
            Assert.Equal(new[] { "CheckA", "CheckB" },
                RiskCategoryMassFireGuard.ParseFiredCheckTypes("CheckA:P0;CheckB:P2"));

        [Fact]
        public void Trims_surrounding_whitespace_around_tokens_and_types() =>
            Assert.Equal(new[] { "CheckA", "CheckB" },
                RiskCategoryMassFireGuard.ParseFiredCheckTypes("  CheckA : P0 ,  CheckB :P2  "));

        [Fact]
        public void Accepts_a_bare_check_name_with_no_severity() =>
            Assert.Equal(new[] { "CheckA" },
                RiskCategoryMassFireGuard.ParseFiredCheckTypes("CheckA"));

        [Fact]
        public void Skips_empty_tokens_between_separators() =>
            Assert.Equal(new[] { "CheckA", "CheckB" },
                RiskCategoryMassFireGuard.ParseFiredCheckTypes(",CheckA:P0,,;CheckB:P2,;"));

        [Fact]
        public void Keeps_duplicate_check_names_because_de_duplication_happens_upstream() =>
            Assert.Equal(new[] { "CheckA", "CheckA" },
                RiskCategoryMassFireGuard.ParseFiredCheckTypes("CheckA:P0,CheckA:P2"));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(",")]
        [InlineData(", ; ,")]
        public void Yields_nothing_for_blank_or_separator_only_input(string breakdown) =>
            Assert.Empty(RiskCategoryMassFireGuard.ParseFiredCheckTypes(breakdown));

        [Fact]
        public void A_token_that_starts_with_a_colon_becomes_its_own_type()
        {
            // ":P0" has its colon at index 0, so `colon > 0` is false and the whole token
            // becomes the type. Pinned as current behaviour.
            Assert.Equal(new[] { ":P0" },
                RiskCategoryMassFireGuard.ParseFiredCheckTypes(":P0"));
        }

        [Fact]
        public void Keeps_only_the_text_before_the_first_colon() =>
            Assert.Equal(new[] { "CheckA" },
                RiskCategoryMassFireGuard.ParseFiredCheckTypes("CheckA:P0:extra"));

        [Fact]
        public void Is_lazily_evaluated_but_repeatable()
        {
            var sequence = RiskCategoryMassFireGuard.ParseFiredCheckTypes("CheckA:P0,CheckB:P2");

            Assert.Equal(sequence.ToList(), sequence.ToList());
        }
    }

    public class RiskCategoryMassFireGuardEvaluateTests
    {
        private static List<RiskCategory> Rows(int count, string severityBreakdown) =>
            Enumerable.Range(1, count)
                .Select(i => new RiskCategory { InterviewId = i, SeverityBreakdown = severityBreakdown })
                .ToList();

        private static List<RiskCategory> Rows(params string[] breakdowns) =>
            breakdowns.Select((b, i) => new RiskCategory { InterviewId = i + 1, SeverityBreakdown = b }).ToList();

        // ── Sample-size gate ──────────────────────────────────────────────────

        [Fact]
        public void Does_not_fire_for_a_null_result_set()
        {
            var verdict = RiskCategoryMassFireGuard.Evaluate(null);

            Assert.False(verdict.ShouldAbort);
            Assert.Equal(0, verdict.SampleSize);
            Assert.Null(verdict.Message);
        }

        [Fact]
        public void Does_not_fire_for_an_empty_result_set()
        {
            var verdict = RiskCategoryMassFireGuard.Evaluate(new List<RiskCategory>());

            Assert.False(verdict.ShouldAbort);
            Assert.Equal(0, verdict.SampleSize);
        }

        [Fact]
        public void Does_not_fire_at_exactly_the_minimum_sample_size()
        {
            // The guard requires sample > minSample, not >=.
            var verdict = RiskCategoryMassFireGuard.Evaluate(
                Rows(DmIvAutoQcAuditConstants.MassFireMinSample, "CheckA:P0"));

            Assert.False(verdict.ShouldAbort);
            Assert.Equal(DmIvAutoQcAuditConstants.MassFireMinSample, verdict.SampleSize);
            Assert.Null(verdict.CheckName);
        }

        [Fact]
        public void Fires_one_row_above_the_minimum_sample_size_when_every_row_trips_the_same_check()
        {
            var verdict = RiskCategoryMassFireGuard.Evaluate(
                Rows(DmIvAutoQcAuditConstants.MassFireMinSample + 1, "CheckA:P0"));

            Assert.True(verdict.ShouldAbort);
            Assert.Equal("CheckA", verdict.CheckName);
            Assert.Equal(100d, verdict.FireRatePct);
        }

        [Fact]
        public void Honours_a_custom_minimum_sample_size()
        {
            var rows = Rows(5, "CheckA:P0");

            Assert.False(RiskCategoryMassFireGuard.Evaluate(rows, minSample: 5).ShouldAbort);
            Assert.True(RiskCategoryMassFireGuard.Evaluate(rows, minSample: 4).ShouldAbort);
        }

        // ── Threshold ─────────────────────────────────────────────────────────

        [Fact]
        public void Does_not_fire_exactly_at_the_threshold()
        {
            // 40 of 100 rows = 40%, and the guard requires strictly greater than 40.
            var rows = Rows(Enumerable.Range(1, 100)
                .Select(i => i <= 40 ? "CheckA:P0" : (string)null)
                .ToArray());

            var verdict = RiskCategoryMassFireGuard.Evaluate(rows, thresholdPct: 40, minSample: 20);

            Assert.False(verdict.ShouldAbort);
            Assert.Equal(40d, verdict.FireRatePct);
            Assert.Null(verdict.Message);
        }

        [Fact]
        public void Fires_one_row_above_the_threshold()
        {
            var rows = Rows(Enumerable.Range(1, 100)
                .Select(i => i <= 41 ? "CheckA:P0" : (string)null)
                .ToArray());

            var verdict = RiskCategoryMassFireGuard.Evaluate(rows, thresholdPct: 40, minSample: 20);

            Assert.True(verdict.ShouldAbort);
            Assert.Equal("CheckA", verdict.CheckName);
            Assert.Equal(41, verdict.FireCount);
            Assert.Equal(41d, verdict.FireRatePct);
        }

        [Fact]
        public void Honours_a_custom_threshold()
        {
            var rows = Rows(Enumerable.Range(1, 100)
                .Select(i => i <= 30 ? "CheckA:P0" : (string)null)
                .ToArray());

            Assert.False(RiskCategoryMassFireGuard.Evaluate(rows, thresholdPct: 30, minSample: 20).ShouldAbort);
            Assert.True(RiskCategoryMassFireGuard.Evaluate(rows, thresholdPct: 29, minSample: 20).ShouldAbort);
        }

        [Fact]
        public void A_hundred_percent_threshold_can_never_fire()
        {
            var verdict = RiskCategoryMassFireGuard.Evaluate(
                Rows(50, "CheckA:P0"), thresholdPct: 100, minSample: 20);

            Assert.False(verdict.ShouldAbort);
        }

        // ── Worst-check selection ─────────────────────────────────────────────

        [Fact]
        public void Reports_the_worst_offending_check()
        {
            var rows = Rows(Enumerable.Range(1, 100)
                .Select(i => i <= 90 ? "Noisy:P0,Quiet:P4" : "Quiet:P4")
                .ToArray());

            var verdict = RiskCategoryMassFireGuard.Evaluate(rows, minSample: 20);

            Assert.True(verdict.ShouldAbort);
            Assert.Equal("Quiet", verdict.CheckName);   // 100% beats 90%
            Assert.Equal(100, verdict.FireCount);
        }

        [Fact]
        public void Counts_a_check_once_per_interview_even_when_it_appears_twice_in_one_breakdown()
        {
            var rows = Rows(50, "CheckA:P0,CheckA:P2,CheckA:P4");

            var verdict = RiskCategoryMassFireGuard.Evaluate(rows, minSample: 20);

            Assert.Equal(50, verdict.FireCount);
            Assert.Equal(100d, verdict.FireRatePct);
        }

        [Fact]
        public void Check_names_are_matched_case_insensitively()
        {
            var rows = Rows(new[] { "CheckA:P0" }
                .Concat(Enumerable.Repeat("checka:P2", 49))
                .ToArray());

            var verdict = RiskCategoryMassFireGuard.Evaluate(rows, minSample: 20);

            Assert.Equal(50, verdict.FireCount);
            Assert.Equal(100d, verdict.FireRatePct);
        }

        [Fact]
        public void Rows_with_no_breakdown_count_towards_the_sample_but_not_the_fire_count()
        {
            var rows = Rows(Enumerable.Range(1, 100)
                .Select(i => i <= 50 ? "CheckA:P0" : (string)null)
                .ToArray());

            var verdict = RiskCategoryMassFireGuard.Evaluate(rows, minSample: 20);

            Assert.Equal(100, verdict.SampleSize);
            Assert.Equal(50, verdict.FireCount);
            Assert.Equal(50d, verdict.FireRatePct);
            Assert.True(verdict.ShouldAbort);
        }

        [Fact]
        public void A_result_set_where_nothing_fired_reports_no_check()
        {
            var rows = Rows(50, null);

            var verdict = RiskCategoryMassFireGuard.Evaluate(rows, minSample: 20);

            Assert.False(verdict.ShouldAbort);
            Assert.Null(verdict.CheckName);
            Assert.Equal(0, verdict.FireCount);
            Assert.Equal(0d, verdict.FireRatePct);
            Assert.Equal(50, verdict.SampleSize);
        }

        [Fact]
        public void Tolerates_null_rows_inside_the_result_set()
        {
            var rows = Rows(50, "CheckA:P0");
            rows.Add(null);

            var verdict = RiskCategoryMassFireGuard.Evaluate(rows, minSample: 20);

            Assert.Equal(51, verdict.SampleSize);
            Assert.Equal(50, verdict.FireCount);
        }

        // ── Message ───────────────────────────────────────────────────────────

        [Fact]
        public void Abort_message_names_the_check_the_rate_and_the_sample_size()
        {
            var rows = Rows(50, "CheckA:P0");

            var verdict = RiskCategoryMassFireGuard.Evaluate(rows, minSample: 20);

            Assert.Equal("aborted: CheckA fired on 100% of 50 interviews — investigate", verdict.Message);
        }

        [Fact]
        public void Abort_message_rounds_the_rate_to_a_whole_percent()
        {
            var rows = Rows(Enumerable.Range(1, 60)
                .Select(i => i <= 41 ? "CheckA:P0" : (string)null)
                .ToArray());

            var verdict = RiskCategoryMassFireGuard.Evaluate(rows, minSample: 20);

            // 41 / 60 = 68.33%
            Assert.True(verdict.ShouldAbort);
            Assert.Contains("fired on 68% of 60 interviews", verdict.Message);
        }

        [Fact]
        public void No_message_is_produced_when_the_run_is_allowed_to_proceed()
        {
            var rows = Rows(Enumerable.Range(1, 100)
                .Select(i => i <= 10 ? "CheckA:P0" : (string)null)
                .ToArray());

            var verdict = RiskCategoryMassFireGuard.Evaluate(rows, minSample: 20);

            Assert.False(verdict.ShouldAbort);
            Assert.Null(verdict.Message);
            Assert.Equal("CheckA", verdict.CheckName);   // still reported for diagnostics
            Assert.Equal(10d, verdict.FireRatePct);
        }

        [Fact]
        public void Uses_the_production_defaults_when_no_overrides_are_supplied()
        {
            // Default threshold 40%, minimum sample 20.
            var justUnder = Rows(Enumerable.Range(1, 100)
                .Select(i => i <= 40 ? "CheckA:P0" : (string)null)
                .ToArray());
            var justOver = Rows(Enumerable.Range(1, 100)
                .Select(i => i <= 41 ? "CheckA:P0" : (string)null)
                .ToArray());

            Assert.False(RiskCategoryMassFireGuard.Evaluate(justUnder).ShouldAbort);
            Assert.True(RiskCategoryMassFireGuard.Evaluate(justOver).ShouldAbort);
        }
    }
}
