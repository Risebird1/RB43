using Risebird.Entities.Data.PlivoCXModels;
using System;
using System.Globalization;

namespace Risebird.Business.PlivoCX
{
    /// <summary>
    /// The pure rules behind PlivoCX campaigns - the per-candidate state machine, the calling
    /// window and time zones - kept free of I/O so they can be unit tested.
    /// </summary>
    public static class PlivoCXCampaignRules
    {
        public const string IndiaTimeZoneId = "Asia/Kolkata";

        /// <summary>
        /// The candidate's state once a call has ended (handoff §11f):
        ///   success  -> Booked     (a slot agent booked; a confirm-only agent got a yes)
        ///   answered -> NotBooked  (picked up but declined / no slot)
        ///   not answered, attempts left -> Pending, retry after Min gap
        ///   not answered, attempts used -> Cold
        /// <paramref name="attempts"/> already counts this call (it is bumped when dialled).
        /// </summary>
        public static PlivoCXCandidateTransition NextState(
            bool collectsSlot, PlivoCXCallLog callLog, bool candidateConfirmed,
            int attempts, int totalAttempts, int minGapMinutes, DateTime nowUtc,
            string originalInterviewDate = null)
        {
            bool answered = IsAnswered(callLog);
            bool success = collectsSlot
                ? callLog.InterviewBooked || callLog.BookingApiSuccess
                : answered && (candidateConfirmed
                               || string.Equals(callLog.CandidateDisposition, "interested", StringComparison.OrdinalIgnoreCase));

            var t = new PlivoCXCandidateTransition
            {
                Disposition = !string.IsNullOrWhiteSpace(callLog.CandidateDisposition)
                    ? callLog.CandidateDisposition
                    : NormaliseOutcome(callLog.CallStatus)
            };

            if (success)
            {
                t.State = PlivoCXCampaignCandidateState.Booked;
                t.BookedSlot = collectsSlot
                    ? BookedSlotText(callLog)
                    : "Confirmed" + (string.IsNullOrWhiteSpace(originalInterviewDate) ? "" : " · " + originalInterviewDate);
            }
            else if (answered)
            {
                t.State = PlivoCXCampaignCandidateState.NotBooked;
            }
            else if (attempts >= totalAttempts)
            {
                t.State = PlivoCXCampaignCandidateState.Cold;
            }
            else
            {
                t.State = PlivoCXCampaignCandidateState.Pending;
                t.NextAttemptAt = nowUtc.AddMinutes(Math.Max(0, minGapMinutes));
            }
            return t;
        }

        /// <summary>The candidate picked up: the call completed, or the agent extracted a disposition.</summary>
        public static bool IsAnswered(PlivoCXCallLog callLog) =>
            string.Equals(callLog.CallStatus, "completed", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(callLog.CandidateDisposition);

        public static string BookedSlotText(PlivoCXCallLog callLog)
        {
            if (callLog.BookedInterviewDateTime.HasValue)
                return callLog.BookedInterviewDateTime.Value.ToString("dd MMM yyyy, h:mm tt", CultureInfo.InvariantCulture);
            var raw = ((callLog.BookedInterviewDate ?? "") + " " + (callLog.BookedInterviewTime ?? "")).Trim();
            return raw.Length == 0 ? null : (raw.Length > 60 ? raw.Substring(0, 60) : raw);
        }

        private static string NormaliseOutcome(string callStatus)
        {
            if (string.IsNullOrWhiteSpace(callStatus)) return "no-answer";
            var s = callStatus.Trim().ToLowerInvariant();
            return s.Length > 30 ? s.Substring(0, 30) : s;
        }

        /// <summary>
        /// Whether a local time of day falls in [opens, closes). A window whose close is before its
        /// open runs overnight (e.g. 20:00-02:00); equal open and close means all day.
        /// </summary>
        public static bool IsWithinWindow(TimeSpan timeOfDay, TimeSpan opens, TimeSpan closes)
        {
            if (opens == closes) return true;
            return opens < closes
                ? timeOfDay >= opens && timeOfDay < closes
                : timeOfDay >= opens || timeOfDay < closes;
        }

        /// <summary>
        /// Resolves an IANA ("Asia/Kolkata") or Windows ("India Standard Time") id on any OS,
        /// falling back to IST - the zone every other Risebird calling job assumes.
        /// </summary>
        public static TimeZoneInfo ResolveTimeZone(string id)
        {
            foreach (var candidate in new[] { id, IndiaTimeZoneId })
            {
                if (string.IsNullOrWhiteSpace(candidate)) continue;
                try { return TimeZoneInfo.FindSystemTimeZoneById(candidate.Trim()); }
                catch (Exception) { }
                try
                {
                    if (TimeZoneInfo.TryConvertIanaIdToWindowsId(candidate.Trim(), out var win))
                        return TimeZoneInfo.FindSystemTimeZoneById(win);
                    if (TimeZoneInfo.TryConvertWindowsIdToIanaId(candidate.Trim(), out var iana))
                        return TimeZoneInfo.FindSystemTimeZoneById(iana);
                }
                catch (Exception) { }
            }
            return TimeZoneInfo.CreateCustomTimeZone(IndiaTimeZoneId, TimeSpan.FromMinutes(330), "India Standard Time", "India Standard Time");
        }

        /// <summary>True when the id names a time zone this server knows (IANA or Windows).</summary>
        public static bool IsKnownTimeZone(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            try { TimeZoneInfo.FindSystemTimeZoneById(id.Trim()); return true; }
            catch (Exception) { }
            return TimeZoneInfo.TryConvertIanaIdToWindowsId(id.Trim(), out _) || TimeZoneInfo.TryConvertWindowsIdToIanaId(id.Trim(), out _);
        }

        /// <summary>Midnight today in <paramref name="tz"/>, as UTC - the start of the Attempts/day count.</summary>
        public static DateTime LocalDayStartUtc(DateTime nowUtc, TimeZoneInfo tz)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), tz);
            var midnight = DateTime.SpecifyKind(local.Date, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(midnight, tz);
        }

        /// <summary>Whether a Running campaign may dial now, and why not.</summary>
        public static string WhyNotDialling(PlivoCXCampaign c, DateTime nowUtc, TimeZoneInfo tz)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), tz);
            if (c.StartsOn.HasValue && local.Date < c.StartsOn.Value.Date) return "before StartsOn";
            if (!IsWithinWindow(local.TimeOfDay, c.WindowOpens, c.WindowCloses)) return "outside the calling window";
            return null;
        }

        /// <summary>EndsOn has passed in the campaign's time zone.</summary>
        public static bool HasEnded(PlivoCXCampaign c, DateTime nowUtc, TimeZoneInfo tz)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), tz);
            return c.EndsOn.HasValue && local.Date > c.EndsOn.Value.Date;
        }

        /// <summary>Policy validation for SavePolicy. Returns an error message, or null when valid.</summary>
        public static string ValidatePolicy(PlivoCXCampaign c)
        {
            if (c.AttemptsPerDay < 1 || c.AttemptsPerDay > 20) return "Attempts / day must be between 1 and 20.";
            if (c.TotalAttempts < 1 || c.TotalAttempts > 50) return "Total attempts must be between 1 and 50.";
            if (c.MinGapMinutes < 0 || c.MinGapMinutes > 10080) return "Minimum gap must be between 0 and 10080 minutes.";
            if (c.ConcurrentCalls < 1 || c.ConcurrentCalls > 50) return "Concurrent calls must be between 1 and 50.";
            if (!IsKnownTimeZone(c.TimeZone)) return "Unknown time zone '" + c.TimeZone + "'. Use an id such as Asia/Kolkata.";
            if (c.StartsOn.HasValue && c.EndsOn.HasValue && c.EndsOn.Value.Date < c.StartsOn.Value.Date) return "Ends on cannot be before Starts on.";
            return null;
        }
    }
}
