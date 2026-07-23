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
            if (string.IsNullOrEmpty(p) || p == "—" || p == "-")
                return Low;
            return FromSeverity(p) + " · " + p;
        }
    }
}
