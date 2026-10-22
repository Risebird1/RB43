using System;
using System.Linq;

namespace Risebird.Business.PlivoCX
{
    /// <summary>
    /// Reads the values a Plivo CX agent extracts at the end of a call. They are written by an LLM,
    /// so the same answer arrives as <c>true</c>, <c>"true"</c>, <c>True.</c>, <c>yes</c> or
    /// <c>Interested</c>; an exact-match check silently turned real confirmations into "not
    /// confirmed". Used by PlivoCXInterviewCallController.HangupCallback for every agent.
    /// </summary>
    public static class PlivoCXOutcomeParser
    {
        private static readonly string[] YesWords = { "true", "yes", "y", "1", "confirmed", "haan", "haan ji", "ji" };

        /// <summary>Whether the extracted <c>candidate_confirmed</c> value means yes.</summary>
        public static bool IsConfirmed(string raw)
        {
            var v = Clean(raw);
            return v.Length > 0 && YesWords.Contains(v);
        }

        /// <summary>
        /// Maps the extracted <c>candidate_disposition</c> to interested | uninterested | cant_say,
        /// or null when nothing was extracted. Negative forms are checked first, since
        /// "not interested" contains "interested".
        /// </summary>
        public static string NormaliseDisposition(string raw)
        {
            var v = Clean(raw).Replace('-', ' ').Replace('_', ' ');
            if (v.Length == 0) return null;
            if (v.Contains("uninterested") || v.Contains("not interested") || v.Contains("disinterested")
                || v.Contains("no longer") || v.Contains("declin"))
                return "uninterested";
            if (v.Contains("cant say") || v.Contains("can t say") || v.Contains("can't say") || v.Contains("cannot say")
                || v.Contains("unsure") || v.Contains("unclear") || v.Contains("unknown"))
                return "cant_say";
            if (v.Contains("interested")) return "interested";
            return "cant_say";
        }

        /// <summary>Lower-cases and strips whitespace, quotes and trailing punctuation.</summary>
        private static string Clean(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return raw.Trim().Trim('"', '\'', '`', '.', '!', ',', ';', ':', ' ').Trim().ToLowerInvariant();
        }
    }
}
