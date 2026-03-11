using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// View model for candidates eligible for Vapi automated interview calls
    /// </summary>
    public class VapiEligibleCandidateVM
    {
        public int CandidateId { get; set; }
        public int IRFId { get; set; }
        public string IRFName { get; set; }
        public string CandidateName { get; set; }
        public string CandidateEmail { get; set; }
        public string CandidatePhone { get; set; }
        public string JobTitle { get; set; }
        public string ClientName { get; set; }
        public int CallAttempts { get; set; }
        public DateTime? LastCallAttempt { get; set; }

        // Plivo CX-specific fields (populated by usp_GetEligibleCandidatesForPlivoCXCalls / usp_GetEligibleCandidatesForPlivoCXReschedule)
        public string PlivoCXDisposition { get; set; }
        public int CompletedCallCount { get; set; }

        /// <summary>
        /// Populated only for reschedule-eligible candidates.
        /// Date of the most recent failed/cancelled interview (e.g. "07 Mar 2026").
        /// Passed to the Plivo CX agent as context so it can reference the original appointment.
        /// </summary>
        public string OriginalInterviewDate { get; set; }
    }
}
