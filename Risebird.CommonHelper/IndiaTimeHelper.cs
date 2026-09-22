using System;
using System.Globalization;

namespace Risebird.CommonHelper
{
    /// <summary>Converts stored UTC <see cref="DateTime"/> values to India Standard Time (Asia/Kolkata) for display or reporting.</summary>
    public static class IndiaTimeHelper
    {
        private const string AsiaKolkata = "Asia/Kolkata";
        private const string IndiaStandardTime = "India Standard Time";

        /// <summary>UTC instant → IST wall clock. Use when the DB column is stored as UTC.</summary>
        public static DateTime UtcToIndiaWallClock(DateTime utc)
        {
            if (utc.Kind == DateTimeKind.Local)
                utc = utc.ToUniversalTime();
            else if (utc.Kind == DateTimeKind.Unspecified)
                utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);

            try
            {
                if (TimeZoneInfo.TryFindSystemTimeZoneById(AsiaKolkata, out var kolkata))
                    return TimeZoneInfo.ConvertTimeFromUtc(utc, kolkata);
                if (TimeZoneInfo.TryFindSystemTimeZoneById(IndiaStandardTime, out var winIst))
                    return TimeZoneInfo.ConvertTimeFromUtc(utc, winIst);
            }
            catch
            {
                /* use offset */
            }

            return utc + TimeSpan.FromHours(5) + TimeSpan.FromMinutes(30);
        }

        /// <summary>Formats a UTC timestamp as IST in one step (e.g. Razor, emails).</summary>
        public static string FormatIstForDisplay(
            DateTime createdAtUtc,
            string format = "dd MMM yyyy, hh:mm tt 'IST'")
        {
            return UtcToIndiaWallClock(createdAtUtc).ToString(format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Nullable convenience overload, for the many display fields that simply have no
        /// timestamp yet. Returns null so the caller's own "—" placeholder still applies.
        /// </summary>
        public static string FormatIstForDisplay(
            DateTime? utc,
            string format = "dd MMM yyyy, hh:mm tt 'IST'")
        {
            return utc.HasValue ? FormatIstForDisplay(utc.Value, format) : null;
        }

        /// <summary>UTC instant as ISO-8601 with Z suffix for client-side parsing.</summary>
        public static string ToUtcIso8601(DateTime? utc)
        {
            if (!utc.HasValue) return string.Empty;
            return DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc).ToString("o", CultureInfo.InvariantCulture);
        }
    }
}
