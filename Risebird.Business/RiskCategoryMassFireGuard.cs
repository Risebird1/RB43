using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Risebird.Business
{
    /// <summary>
    /// Backfill-only circuit breaker: aborts a risk re-grade when any single check
    /// fires on an implausibly high share of interviews in the run.
    /// </summary>
    public static class RiskCategoryMassFireGuard
    {
        public sealed class Verdict
        {
            public bool ShouldAbort { get; set; }
            public string CheckName { get; set; }
            public double FireRatePct { get; set; }
            public int SampleSize { get; set; }
            public int FireCount { get; set; }
            public string Message { get; set; }
        }

        /// <summary>
        /// Parses SeverityBreakdown tokens of the form "CheckType:P0,Other:P2".
        /// </summary>
        public static IEnumerable<string> ParseFiredCheckTypes(string severityBreakdown)
        {
            if (string.IsNullOrWhiteSpace(severityBreakdown))
                yield break;

            foreach (var part in severityBreakdown.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var token = part.Trim();
                if (token.Length == 0)
                    continue;

                var colon = token.IndexOf(':');
                var type = colon > 0 ? token.Substring(0, colon).Trim() : token;
                if (type.Length > 0)
                    yield return type;
            }
        }

        public static Verdict Evaluate(
            IReadOnlyList<RiskCategory> dryRunResults,
            int thresholdPct = DmIvAutoQcAuditConstants.MassFireThresholdPct,
            int minSample = DmIvAutoQcAuditConstants.MassFireMinSample)
        {
            var sample = dryRunResults?.Count ?? 0;
            if (sample <= minSample)
            {
                return new Verdict
                {
                    ShouldAbort = false,
                    SampleSize = sample,
                    Message = null
                };
            }

            var fireCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in dryRunResults)
            {
                foreach (var check in ParseFiredCheckTypes(row?.SeverityBreakdown).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!fireCounts.TryGetValue(check, out var n))
                        n = 0;
                    fireCounts[check] = n + 1;
                }
            }

            string worstCheck = null;
            var worstCount = 0;
            double worstPct = 0;
            foreach (var kv in fireCounts)
            {
                var pct = (100.0 * kv.Value) / sample;
                if (pct > worstPct || (Math.Abs(pct - worstPct) < 0.0001 && kv.Value > worstCount))
                {
                    worstPct = pct;
                    worstCount = kv.Value;
                    worstCheck = kv.Key;
                }
            }

            if (worstCheck == null || worstPct <= thresholdPct)
            {
                return new Verdict
                {
                    ShouldAbort = false,
                    SampleSize = sample,
                    CheckName = worstCheck,
                    FireCount = worstCount,
                    FireRatePct = worstPct
                };
            }

            var rateText = worstPct.ToString("0", CultureInfo.InvariantCulture);
            return new Verdict
            {
                ShouldAbort = true,
                SampleSize = sample,
                CheckName = worstCheck,
                FireCount = worstCount,
                FireRatePct = worstPct,
                Message = $"aborted: {worstCheck} fired on {rateText}% of {sample} interviews — investigate"
            };
        }
    }
}
