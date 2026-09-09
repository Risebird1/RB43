using System;
using System.Collections.Generic;
using Risebird.Entities.Enum;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Cumulative risk scoring for DesktopProctor session events: severity weight x confidence
    /// multiplier, summed across events and clamped to [0, 100] — mirrors the weighting the
    /// desktop client itself uses (see the API contract's RiskScorer.cs reference).
    /// </summary>
    public static class ProctoringRiskScorer
    {
        private static readonly IReadOnlyDictionary<ProctoringSeverity, int> SeverityWeight = new Dictionary<ProctoringSeverity, int>
        {
            [ProctoringSeverity.Info] = 0,
            [ProctoringSeverity.Low] = 5,
            [ProctoringSeverity.Medium] = 15,
            [ProctoringSeverity.High] = 30,
        };

        private static readonly IReadOnlyDictionary<ProctoringConfidence, double> ConfidenceMultiplier = new Dictionary<ProctoringConfidence, double>
        {
            [ProctoringConfidence.Low] = 0.5,
            [ProctoringConfidence.Medium] = 0.75,
            [ProctoringConfidence.High] = 1.0,
        };

        public static int ScoreEvent(ProctoringSeverity level, ProctoringConfidence confidence)
        {
            var weight = SeverityWeight.TryGetValue(level, out var w) ? w : 0;
            var multiplier = ConfidenceMultiplier.TryGetValue(confidence, out var m) ? m : 1.0;
            return (int)Math.Round(weight * multiplier, MidpointRounding.AwayFromZero);
        }

        public static int Clamp(int score) => Math.Clamp(score, 0, 100);
    }
}
