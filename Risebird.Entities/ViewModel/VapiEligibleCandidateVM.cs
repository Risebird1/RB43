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
    }
}
