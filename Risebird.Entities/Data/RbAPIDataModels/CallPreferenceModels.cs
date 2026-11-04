using System;
using System.Collections.Generic;

namespace Risebird.Entities.Data.RbAPIDataModels
{
    /// <summary>
    /// Call opt-out signal for an interviewer (platform level), parsed from the provider payload
    /// posted to api/CallPreference/Interviewer/OptOut, or built by the portal flag panel.
    /// </summary>
    public class InterviewerCallOptOutRequest
    {
        public int? UserId { get; set; }
        public string Phone { get; set; }
        public string EmailId { get; set; }
        /// <summary>Plivo | ElevenLabs | Portal | Unknown, or the ?source= value.</summary>
        public string Source { get; set; }
        /// <summary>AICall | HumanCall.</summary>
        public string OptOutSource { get; set; } = "AICall";
        public string ProviderCallId { get; set; }
        public string Reason { get; set; }
        public string RawPayload { get; set; }
        public string CreatedBy { get; set; }
    }

    /// <summary>
    /// Call opt-out signal for a candidate (IRF / skill channel level), posted to api/CallPreference/Candidate/OptOut.
    /// Without CandidateId or IRFId the event is only logged.
    /// </summary>
    public class CandidateCallOptOutRequest
    {
        public int? CandidateId { get; set; }
        /// <summary>IRF.id (numeric), not the IRF code.</summary>
        public int? IRFId { get; set; }
        public string Phone { get; set; }
        public string EmailId { get; set; }
        public string Source { get; set; }
        public string OptOutSource { get; set; } = "AICall";
        public string ProviderCallId { get; set; }
        public string Reason { get; set; }
        public string RawPayload { get; set; }
        public string CreatedBy { get; set; }
    }

    public class CallOptOutResult
    {
        /// <summary>Rows matched and opted out; 0 = logged as unmatched.</summary>
        public int OptedOut { get; set; }
        public int? IRFId { get; set; }
        /// <summary>Candidate opt-outs: the interviews Usp_ColdCandidateMarked cancelled, for the cancellation mails.</summary>
        public List<CancelledInterview> CancelledInterviews { get; set; } = new List<CancelledInterview>();
    }

    public class CancelledInterview
    {
        public int InterviewId { get; set; }
        /// <summary>NULL when the interviewer is inactive (no mail).</summary>
        public string Interviewer { get; set; }
        public DateTime InterviewDate { get; set; }
        public string CandidateName { get; set; }
        /// <summary>IRF.id</summary>
        public int IRFId { get; set; }
    }

    public class CanCallResult
    {
        public bool CanCall { get; set; }
        public string Reason { get; set; }
    }

    public class CallConsentResult
    {
        public bool ConsentRecorded { get; set; }
        public bool WasOptedOut { get; set; }
    }
}
