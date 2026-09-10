using Risebird.Entities.Enum;

namespace Risebird.CommonHelper.Tests.Helpers
{
    public class ProctoringRiskScorerScoreEventTests
    {
        // weight × multiplier, rounded away from zero:
        //   Info 0 | Low 5 | Medium 15 | High 30
        //   Low ×0.5 | Medium ×0.75 | High ×1.0
        [Theory]
        [InlineData(ProctoringSeverity.Info, ProctoringConfidence.Low, 0)]
        [InlineData(ProctoringSeverity.Info, ProctoringConfidence.Medium, 0)]
        [InlineData(ProctoringSeverity.Info, ProctoringConfidence.High, 0)]
        [InlineData(ProctoringSeverity.Low, ProctoringConfidence.Low, 3)]      // 2.5 → 3 (away from zero)
        [InlineData(ProctoringSeverity.Low, ProctoringConfidence.Medium, 4)]   // 3.75 → 4
        [InlineData(ProctoringSeverity.Low, ProctoringConfidence.High, 5)]
        [InlineData(ProctoringSeverity.Medium, ProctoringConfidence.Low, 8)]   // 7.5 → 8
        [InlineData(ProctoringSeverity.Medium, ProctoringConfidence.Medium, 11)] // 11.25 → 11
        [InlineData(ProctoringSeverity.Medium, ProctoringConfidence.High, 15)]
        [InlineData(ProctoringSeverity.High, ProctoringConfidence.Low, 15)]
        [InlineData(ProctoringSeverity.High, ProctoringConfidence.Medium, 23)] // 22.5 → 23
        [InlineData(ProctoringSeverity.High, ProctoringConfidence.High, 30)]
        public void Scores_every_severity_and_confidence_pair(
            ProctoringSeverity level, ProctoringConfidence confidence, int expected)
        {
            Assert.Equal(expected, ProctoringRiskScorer.ScoreEvent(level, confidence));
        }

        [Fact]
        public void Score_never_goes_negative()
        {
            foreach (ProctoringSeverity level in System.Enum.GetValues<ProctoringSeverity>())
                foreach (ProctoringConfidence confidence in System.Enum.GetValues<ProctoringConfidence>())
                    Assert.True(ProctoringRiskScorer.ScoreEvent(level, confidence) >= 0);
        }

        [Fact]
        public void Score_is_monotonic_in_severity_at_a_fixed_confidence()
        {
            var info = ProctoringRiskScorer.ScoreEvent(ProctoringSeverity.Info, ProctoringConfidence.High);
            var low = ProctoringRiskScorer.ScoreEvent(ProctoringSeverity.Low, ProctoringConfidence.High);
            var medium = ProctoringRiskScorer.ScoreEvent(ProctoringSeverity.Medium, ProctoringConfidence.High);
            var high = ProctoringRiskScorer.ScoreEvent(ProctoringSeverity.High, ProctoringConfidence.High);

            Assert.True(info < low && low < medium && medium < high);
        }

        [Fact]
        public void Score_is_monotonic_in_confidence_at_a_fixed_severity()
        {
            var low = ProctoringRiskScorer.ScoreEvent(ProctoringSeverity.High, ProctoringConfidence.Low);
            var medium = ProctoringRiskScorer.ScoreEvent(ProctoringSeverity.High, ProctoringConfidence.Medium);
            var high = ProctoringRiskScorer.ScoreEvent(ProctoringSeverity.High, ProctoringConfidence.High);

            Assert.True(low < medium && medium < high);
        }

        [Fact]
        public void Unknown_severity_contributes_zero_weight()
        {
            var unknown = (ProctoringSeverity)999;

            Assert.Equal(0, ProctoringRiskScorer.ScoreEvent(unknown, ProctoringConfidence.High));
        }

        [Fact]
        public void Unknown_confidence_falls_back_to_a_full_multiplier()
        {
            var unknown = (ProctoringConfidence)999;

            // Missing multiplier defaults to 1.0, so the raw severity weight comes through.
            Assert.Equal(30, ProctoringRiskScorer.ScoreEvent(ProctoringSeverity.High, unknown));
        }

        [Fact]
        public void Unknown_severity_beats_unknown_confidence_because_weight_is_zero()
        {
            Assert.Equal(0, ProctoringRiskScorer.ScoreEvent((ProctoringSeverity)999, (ProctoringConfidence)999));
        }
    }

    public class ProctoringRiskScorerClampTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(50, 50)]
        [InlineData(99, 99)]
        [InlineData(100, 100)]
        public void Leaves_in_range_scores_untouched(int input, int expected) =>
            Assert.Equal(expected, ProctoringRiskScorer.Clamp(input));

        [Theory]
        [InlineData(101, 100)]
        [InlineData(250, 100)]
        [InlineData(int.MaxValue, 100)]
        public void Clamps_the_upper_bound_to_one_hundred(int input, int expected) =>
            Assert.Equal(expected, ProctoringRiskScorer.Clamp(input));

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(-100, 0)]
        [InlineData(int.MinValue, 0)]
        public void Clamps_the_lower_bound_to_zero(int input, int expected) =>
            Assert.Equal(expected, ProctoringRiskScorer.Clamp(input));

        [Fact]
        public void Is_idempotent()
        {
            foreach (var value in new[] { int.MinValue, -1, 0, 42, 100, 101, int.MaxValue })
                Assert.Equal(
                    ProctoringRiskScorer.Clamp(value),
                    ProctoringRiskScorer.Clamp(ProctoringRiskScorer.Clamp(value)));
        }

        [Fact]
        public void A_full_session_of_worst_case_events_saturates_at_one_hundred()
        {
            var total = 0;
            for (var i = 0; i < 10; i++)
                total += ProctoringRiskScorer.ScoreEvent(ProctoringSeverity.High, ProctoringConfidence.High);

            Assert.Equal(300, total);
            Assert.Equal(100, ProctoringRiskScorer.Clamp(total));
        }
    }
}
