using System;
using System.Text;

namespace Risebird.Entities.Helpers
{
    /// <summary>
    /// Encodes/decodes the Plivo CX availability-call <c>log_id</c> token used for
    /// <b>profile interviewers that do NOT yet have an InterviewerEmailAvailability record</b>.
    ///
    /// An availability record must only be created once the interviewer has CONFIRMED
    /// availability — which, for a Plivo voice call, can only be known after the call
    /// completes with a positive/interested disposition. We therefore do not pre-create
    /// the row before dialing; instead we carry the data needed to create it later
    /// (profile id, MAG name, source) inside the round-tripped <c>log_id</c> value.
    ///
    /// The hangup webhook recognises this token, and creates the record only when the
    /// disposition is a confirmed availability.
    ///
    /// Format: <c>PROF~{profileId}~{base64url(magName)}~{base64url(source)}</c>
    /// Plain integer log_ids continue to map to existing records as before.
    /// </summary>
    public static class AvailabilityCallToken
    {
        private const string Prefix = "PROF~";

        public static string Build(int interviewerProfileId, string magName, string source)
        {
            return $"{Prefix}{interviewerProfileId}~{Encode(magName)}~{Encode(source)}";
        }

        public static bool TryParse(string token, out int interviewerProfileId, out string magName, out string source)
        {
            interviewerProfileId = 0;
            magName = null;
            source = null;

            if (string.IsNullOrWhiteSpace(token) || !token.StartsWith(Prefix, StringComparison.Ordinal))
                return false;

            var parts = token.Substring(Prefix.Length).Split('~');
            if (parts.Length < 3)
                return false;

            if (!int.TryParse(parts[0], out interviewerProfileId) || interviewerProfileId <= 0)
                return false;

            magName = Decode(parts[1]);
            source = Decode(parts[2]);
            return true;
        }

        private static string Encode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // URL-safe base64 so the value never contains the '~' delimiter or other
            // characters Plivo might mangle when round-tripping the log_id.
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
                          .Replace('+', '-')
                          .Replace('/', '_')
                          .TrimEnd('=');
        }

        private static string Decode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var b64 = value.Replace('-', '+').Replace('_', '/');
            switch (b64.Length % 4)
            {
                case 2: b64 += "=="; break;
                case 3: b64 += "="; break;
            }

            try { return Encoding.UTF8.GetString(Convert.FromBase64String(b64)); }
            catch { return string.Empty; }
        }
    }
}
