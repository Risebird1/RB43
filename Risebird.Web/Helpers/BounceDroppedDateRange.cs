using System;

namespace Risebird.Web.Helpers
{
    /// <summary>
    /// Date window of the Bounce &amp; Dropped Mail page. The default is the last 2 days; the team can look back
    /// at most <see cref="MaxDays"/> days. Always computed on the server: the browser's min/max are never trusted.
    /// </summary>
    public static class BounceDroppedDateRange
    {
        public const int DefaultDays = 2;
        public const int MaxDays = 45;
        public static readonly int[] Presets = { 2, 7, 15, 30, 45 };
        public const string ClampNotice = "Data is available for the last 45 days only.";

        /// <summary>Earliest selectable date: a <see cref="MaxDays"/>-day window ending today.</summary>
        public static DateTime EarliestStart(DateTime today) => today.Date.AddDays(-(MaxDays - 1));

        /// <summary>
        /// Preset (rangeDays in <see cref="Presets"/>): end = today, start = today - (rangeDays - 1).
        /// Custom: the given dates, a missing end = today, a missing start = a 2-day window ending at end
        /// (bugs #2, #3). Start is clamped to <see cref="EarliestStart"/>, end to today.
        /// </summary>
        public static (DateTime Start, DateTime End, int? RangeDays, string Notice) Resolve(int? rangeDays, DateTime? startDate, DateTime? endDate, DateTime today)
        {
            today = today.Date;
            var earliest = EarliestStart(today);
            string notice = null;

            if (rangeDays.HasValue && Array.IndexOf(Presets, rangeDays.Value) >= 0)
            {
                return (today.AddDays(-(rangeDays.Value - 1)), today, rangeDays, null);
            }

            if (!startDate.HasValue && !endDate.HasValue)
            {
                return (today.AddDays(-(DefaultDays - 1)), today, DefaultDays, null);
            }

            var end = (endDate ?? today).Date;
            if (end > today) end = today;
            var start = (startDate ?? end.AddDays(-(DefaultDays - 1))).Date;
            if (start > end)
            {
                var swap = start;
                start = end;
                end = swap > today ? today : swap;
            }
            if (start < earliest)
            {
                start = earliest;
                notice = ClampNotice;
            }
            if (end < start) end = start;

            return (start, end, null, notice);
        }

        /// <summary>"Showing 21 Aug 2026 to 5 Oct 2026 (45 days)".</summary>
        public static string Label(DateTime start, DateTime end)
        {
            var days = (int)(end.Date - start.Date).TotalDays + 1;
            return $"Showing {start:d MMM yyyy} to {end:d MMM yyyy} ({days} day{(days == 1 ? "" : "s")})";
        }
    }
}
