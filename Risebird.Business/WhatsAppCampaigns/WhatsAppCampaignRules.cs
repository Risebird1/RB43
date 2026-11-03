using Risebird.Business.PlivoCX;
using Risebird.CommonHelper;
using Risebird.Entities.Data.WhatsAppCampaignModels;
using System;

namespace Risebird.Business.WhatsAppCampaigns
{
    /// <summary>
    /// The pure rules behind WhatsApp campaigns - the per-candidate state after a send, the sending window and
    /// policy bounds - kept free of I/O so they can be unit tested. Window and time-zone handling are the
    /// PlivoCX campaign rules.
    /// </summary>
    public static class WhatsAppCampaignRules
    {
        /// <summary>Interakt failures in a row before a candidate is given up as Failed.</summary>
        public const int MaxFailedSends = 3;

        /// <summary>How long a failed send waits before it is tried again.</summary>
        public static readonly TimeSpan FailureRetryAfter = TimeSpan.FromHours(1);

        /// <summary>
        /// The candidate's state after one send attempt, from the send helper's result:
        ///   sent                                   -> Sent (counts as a send; failures reset)
        ///   skipped: no number                     -> NoNumber
        ///   skipped: opted out / at the limit / …  -> NoResponse, with the reason
        ///   failed, under 3 in a row               -> Pending, retried in an hour
        ///   failed, the 3rd in a row               -> Failed
        /// </summary>
        public static WhatsAppCandidateTransition AfterSend(string result, int failedSendsBefore, DateTime nowUtc)
        {
            if (result == WhatsAppHelper.SentMessage)
                return new WhatsAppCandidateTransition { State = WhatsAppCampaignCandidateState.Sent, Counted = true, FailedSends = 0 };

            if (result == WhatsAppHelper.SkippedNoNumber)
                return new WhatsAppCandidateTransition { State = WhatsAppCampaignCandidateState.NoNumber, FailedSends = failedSendsBefore, OutcomeNote = "No WhatsApp number on file" };

            if (result != null && result.StartsWith("Skipped", StringComparison.OrdinalIgnoreCase))
                return new WhatsAppCandidateTransition { State = WhatsAppCampaignCandidateState.NoResponse, FailedSends = failedSendsBefore, OutcomeNote = SkipNote(result) };

            int failed = failedSendsBefore + 1;
            if (failed >= MaxFailedSends)
                return new WhatsAppCandidateTransition { State = WhatsAppCampaignCandidateState.Failed, FailedSends = failed, OutcomeNote = "Interakt rejected it " + failed + " times in a row" };

            return new WhatsAppCandidateTransition
            {
                State = WhatsAppCampaignCandidateState.Pending,
                FailedSends = failed,
                NextSendAt = nowUtc.Add(FailureRetryAfter)
            };
        }

        /// <summary>The reason a skipped send ended the row, in words for the Outcome column.</summary>
        public static string SkipNote(string result)
        {
            if (result == WhatsAppHelper.SkippedOptedOut) return "Opted out of WhatsApp for this role";
            if (result == WhatsAppHelper.SkippedLimitReached) return "WhatsApp limit reached (3/3) for this IRF";
            var reason = result?.Substring(result.IndexOf(':') + 1).Trim();
            return string.IsNullOrWhiteSpace(reason) ? "Skipped" : char.ToUpperInvariant(reason[0]) + reason.Substring(1);
        }

        /// <summary>Whether a campaign may send now, and why not.</summary>
        public static string WhyNotSending(WhatsAppCampaign c, DateTime nowUtc, TimeZoneInfo tz)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), tz);
            if (c.StartsOn.HasValue && local.Date < c.StartsOn.Value.Date) return "it starts on " + c.StartsOn.Value.ToString("dd MMM yyyy");
            if (c.EndsOn.HasValue && local.Date > c.EndsOn.Value.Date) return "it ended on " + c.EndsOn.Value.ToString("dd MMM yyyy");
            if (!PlivoCXCampaignRules.IsWithinWindow(local.TimeOfDay, c.WindowOpens, c.WindowCloses))
                return "it is outside the sending window (" + c.WindowOpens.ToString(@"hh\:mm") + "–" + c.WindowCloses.ToString(@"hh\:mm") + " " + c.TimeZone + ")";
            return null;
        }

        /// <summary>EndsOn has passed in the campaign's time zone.</summary>
        public static bool HasEnded(WhatsAppCampaign c, DateTime nowUtc, TimeZoneInfo tz)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), tz);
            return c.EndsOn.HasValue && local.Date > c.EndsOn.Value.Date;
        }

        /// <summary>Policy validation for SavePolicy. Returns an error message, or null when valid.</summary>
        public static string ValidatePolicy(WhatsAppCampaign c)
        {
            if (c.MaxSends < 1 || c.MaxSends > 3) return "Max sends must be between 1 and 3.";
            if (c.ResendAfterHours < 4 || c.ResendAfterHours > 168) return "Re-send after must be between 4 and 168 hours.";
            if (c.BatchSize < 1 || c.BatchSize > 200) return "Batch size must be between 1 and 200.";
            if (c.WindowOpens >= c.WindowCloses) return "The sending window must open before it closes.";
            if (!PlivoCXCampaignRules.IsKnownTimeZone(c.TimeZone)) return "Unknown time zone '" + c.TimeZone + "'. Use an id such as Asia/Kolkata.";
            if (c.StartsOn.HasValue && c.EndsOn.HasValue && c.EndsOn.Value.Date < c.StartsOn.Value.Date) return "Ends on cannot be before Starts on.";
            return null;
        }

        /// <summary>The picker's eligibility for one candidate, and why not.</summary>
        public static (string Eligibility, string Note) Eligibility(WhatsAppCampaignEligibilityRow row, string purpose, int sendLimit)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.WhatsAppNumber)) return ("no-number", null);
            if (row.OptedOut) return ("not-eligible", "unsubscribed from WhatsApp for this role");
            if (WhatsAppCampaignPurpose.IsLimited(purpose) && sendLimit > 0 && row.SendsUsed >= sendLimit)
                return ("not-eligible", "WhatsApp limit reached (" + row.SendsUsed + "/" + sendLimit + ")");
            return ("eligible", null);
        }
    }
}
