using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// How a code session ended up. Only Submitted reaches a client report: publishing an
    /// abandoned attempt would damage the candidate for something that was never offered
    /// as an answer.
    /// </summary>
    public enum CodeSessionState
    {
        Generated = 1,
        Opened = 2,
        InProgress = 3,
        Submitted = 4,
        Expired = 5
    }

    /// <summary>
    /// Three outcomes, not two. Failed says something true about the candidate; NotExecuted
    /// says nothing about them at all, and a report must never let the two look alike.
    /// </summary>
    public enum CodeRunState
    {
        Executed = 1,
        Failed = 2,
        NotExecuted = 3
    }

    /// <summary>One attempt: the question as asked, what was written, and what it printed.</summary>
    public class CodeSessionVM
    {
        public int id { get; set; }
        public int InterviewId { get; set; }
        public int? IRFId { get; set; }

        /// <summary>Null for an ad-hoc question that was not saved back to the bank.</summary>
        public int? SampleQuestionId { get; set; }

        public int? SkillId { get; set; }
        public string SkillName { get; set; }

        /// <summary>A snapshot as asked — the bank entry may have been edited since.</summary>
        public string QuestionText { get; set; }

        public string SessionToken { get; set; }
        public int AttemptNumber { get; set; }
        public int SessionState { get; set; }

        public string Language { get; set; }
        public string LanguageVersionUsed { get; set; }

        public string SubmittedCode { get; set; }

        /// <summary>Kept apart from Stderr so a client can see that something printed and something else threw.</summary>
        public string Stdout { get; set; }
        public string Stderr { get; set; }

        public int? ExitStatus { get; set; }
        public int? RunState { get; set; }
        public int? RunDurationMs { get; set; }

        /// <summary>Captured, deliberately not published — see the spec's open questions.</summary>
        public int PasteEvents { get; set; }

        public int? TimeLimitMinutes { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? OpenedAt { get; set; }
        public DateTime? LastSavedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public bool IsSubmitted => SessionState == (int)CodeSessionState.Submitted;

        /// <summary>
        /// Read-only once submitted or once the interview has ended. Each attempt freezes
        /// independently: an earlier session left open must not block a later one.
        /// </summary>
        public bool IsFrozen =>
            IsSubmitted
            || SessionState == (int)CodeSessionState.Expired
            || (ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.Now);

        public CodeRunState RunStateValue =>
            RunState.HasValue && System.Enum.IsDefined(typeof(CodeRunState), RunState.Value)
                ? (CodeRunState)RunState.Value
                : CodeRunState.NotExecuted;

        /// <summary>
        /// The line a client sees when nothing ran. Never a blank panel — blank reads as
        /// "the candidate produced nothing", which is a different and untrue claim.
        /// </summary>
        public string NotExecutedReason =>
            "This code was not executed, so there is no output to show. That is a limitation of the run, not a result produced by the candidate.";
    }

    /// <summary>What the candidate's page needs. Deliberately carries nothing else — not the
    /// candidate, not the channel, not the other sessions in the same interview.</summary>
    public class CodeSessionCandidateVM
    {
        public string SessionToken { get; set; }
        public string QuestionText { get; set; }
        public string SkillName { get; set; }
        public string Language { get; set; }
        public string SubmittedCode { get; set; }
        public string Stdout { get; set; }
        public string Stderr { get; set; }
        public int? RunState { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int SessionState { get; set; }

        public bool IsFrozen =>
            SessionState == (int)CodeSessionState.Submitted
            || SessionState == (int)CodeSessionState.Expired
            || (ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.Now);

        /// <summary>Null when the token did not resolve — the page renders a neutral "not available".</summary>
        public bool IsValid { get; set; }
    }

    /// <summary>A coding question on the alignment page, with whatever sessions it already has.</summary>
    public class CodeQuestionVM
    {
        public CodeQuestionVM()
        {
            Sessions = new List<CodeSessionVM>();
        }

        public int SampleQuestionId { get; set; }
        public int? SkillId { get; set; }
        public string SkillName { get; set; }
        public string QuestionText { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public List<CodeSessionVM> Sessions { get; set; }
    }

}
