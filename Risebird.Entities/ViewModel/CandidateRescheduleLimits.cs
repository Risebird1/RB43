using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// A channel's candidate reschedule cap and completion deadline, as they stand for one interview.
    /// Read by usp_GetCandidateRescheduleLimits. Both limits are null on every channel that has not
    /// set them, and then nothing about rescheduling changes.
    ///
    /// The limits apply to the candidate only. Risebird internal and client users reach the same
    /// scheduling screen and are never capped: see <see cref="IsCandidateActor"/>.
    /// </summary>
    public class CandidateRescheduleLimits
    {
        /// <summary>IRF.MaxCandidateReschedules. Null = unlimited.</summary>
        public int? MaxCandidateReschedules { get; set; }

        /// <summary>IRF.InterviewCompleteByDays. Null = no deadline.</summary>
        public int? InterviewCompleteByDays { get; set; }

        /// <summary>InterviewMeetings.candidateEmail of the interview being rescheduled.</summary>
        public string CandidateEmail { get; set; }

        /// <summary>The interview's current slot (IST, as stored).</summary>
        public DateTime? CurrentInterviewDate { get; set; }

        /// <summary>Candidate-initiated reschedules so far, across the candidate's meetings on this channel.</summary>
        public int CandidateReschedulesUsed { get; set; }

        /// <summary>Earliest scheduled date of the candidate's meetings on this channel (IST, as stored).</summary>
        public DateTime? FirstScheduledDate { get; set; }

        public bool HasRescheduleCap => MaxCandidateReschedules.HasValue && MaxCandidateReschedules.Value >= 0;

        public bool HasDeadline => CompleteByDate.HasValue;

        public bool HasLimits => HasRescheduleCap || HasDeadline;

        public int? RemainingReschedules => HasRescheduleCap
            ? Math.Max(0, MaxCandidateReschedules.Value - CandidateReschedulesUsed)
            : (int?)null;

        public bool IsQuotaSpent => HasRescheduleCap && RemainingReschedules == 0;

        /// <summary>True when the reschedule about to be made is the candidate's last one.</summary>
        public bool IsLastReschedule => HasRescheduleCap && RemainingReschedules == 1;

        /// <summary>Last date (IST) a slot may fall on. Null when there is no deadline.</summary>
        public DateTime? CompleteByDate => InterviewCompleteByDays.HasValue && InterviewCompleteByDays.Value > 0 && FirstScheduledDate.HasValue
            ? FirstScheduledDate.Value.Date.AddDays(InterviewCompleteByDays.Value)
            : (DateTime?)null;

        public bool IsBeyondDeadline(DateTime slotIst) => CompleteByDate.HasValue && slotIst.Date > CompleteByDate.Value;

        /// <summary>
        /// How many days of the slot picker to open, so that no date past the deadline is offered.
        /// </summary>
        public int EffectiveSlotDays(int timeSlotDays, DateTime todayIst)
        {
            if (!CompleteByDate.HasValue)
            {
                return timeSlotDays;
            }
            var daysToDeadline = (CompleteByDate.Value - todayIst.Date).Days + 1;
            return Math.Max(0, Math.Min(timeSlotDays, daysToDeadline));
        }

        /// <summary>
        /// Whether the person acting is the candidate. Gated on the actor, never on the action: the
        /// reschedule screen is shared with Risebird internal and client users.
        ///
        /// No signed-in session means the candidate following their e-mailed link. The candidate's own
        /// public pages (Candidate/VerifiedInterviewDetails, CandidatePublicdetails) also put the
        /// candidate's e-mail into the session, so a session holding this interview's candidate e-mail
        /// is the candidate too - otherwise the cap would stop applying after their first reschedule.
        /// Any other signed-in e-mail is a Risebird or client user and is never capped.
        /// </summary>
        public bool IsCandidateActor(string sessionEmail)
        {
            if (string.IsNullOrWhiteSpace(sessionEmail))
            {
                return true;
            }
            return !string.IsNullOrWhiteSpace(CandidateEmail)
                && string.Equals(sessionEmail.Trim(), CandidateEmail.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
