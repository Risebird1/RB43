using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Client-friendly priority tiers mapped from read-time Trial severity (P0–P5).
    /// Central map — retune here only; sorting stays on raw P.
    /// </summary>
    public static class TrialFlagClientPriority
    {
        public const string Critical = "Critical";
        public const string Medium = "Medium";
        public const string Low = "Low";
        public const string Clear = "Clear";

        public static string FromSeverity(string severity)
        {
            var p = (severity ?? "").Trim().ToUpperInvariant();
            switch (p)
            {
                case "P0":
                case "P1":
                    return Critical;
                case "P2":
                case "P3":
                    return Medium;
                case "P5":
                    return Clear;
                default:
                    return Low;
            }
        }

        /// <summary>CSS modifier: critical | medium | low | clear</summary>
        public static string CssModifier(string severity)
        {
            var tier = FromSeverity(severity);
            if (tier == Critical) return "critical";
            if (tier == Medium) return "medium";
            if (tier == Clear) return "clear";
            return "low";
        }

        public static string BadgeLabel(string severity)
        {
            var p = (severity ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(p) || p == "—" || p == "-" || p == "–")
                return Low;
            return FromSeverity(p) + " · " + p;
        }

        /// <summary>
        /// P0–P5 (+ blank) → tier/css/badge for JS injection so the AJAX grid
        /// cannot drift from this map.
        /// </summary>
        public static IReadOnlyDictionary<string, TrialFlagClientPriorityMeta> BuildJsMap()
        {
            var keys = new[] { "P0", "P1", "P2", "P3", "P4", "P5", "" };
            var map = new Dictionary<string, TrialFlagClientPriorityMeta>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in keys)
            {
                map[key] = new TrialFlagClientPriorityMeta
                {
                    Tier = FromSeverity(key),
                    Css = CssModifier(key),
                    Badge = BadgeLabel(key)
                };
            }
            return map;
        }
    }

    public sealed class TrialFlagClientPriorityMeta
    {
        public string Tier { get; set; }
        public string Css { get; set; }
        public string Badge { get; set; }
    }
}
