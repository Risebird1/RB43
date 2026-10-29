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

        /// <summary>
        /// Plivo rejected the call for account capacity rather than the candidate not answering:
        /// concurrency limit (hangup cause 5030 / SIP trunk 5190, "Concurrency Limit Breached") or
        /// CPS (5180, cps_limit_reached). Such a call must not use up one of the candidate's attempts.
        /// </summary>
        public static bool IsCapacityRejection(string hangupCause)
        {
            if (string.IsNullOrWhiteSpace(hangupCause)) return false;
            var v = hangupCause.ToLowerInvariant();
            return v.Contains("5030") || v.Contains("5190") || v.Contains("5180")
                || v.Contains("concurrency limit") || v.Contains("cps_limit") || v.Contains("cps limit")
                // ElevenLabs answers an outbound call over its plan's concurrency with HTTP 429.
                || v.Contains("http 429") || v.Contains("too many concurrent") || v.Contains("concurrency_limit");
        }

        /// <summary>Lower-cases and strips whitespace, quotes and trailing punctuation.</summary>
        private static string Clean(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return raw.Trim().Trim('"', '\'', '`', '.', '!', ',', ';', ':', ' ').Trim().ToLowerInvariant();
        }
    }
}
