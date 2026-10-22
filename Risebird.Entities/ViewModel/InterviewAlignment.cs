using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Row returned by usp_GetInterviewAlignmentContext: what the interview alignment page needs beyond
    /// the Meeting that usp_FetchAudioComment already binds.
    /// </summary>
    public class InterviewAlignmentContext
    {
        public int IRFIntId { get; set; }
        public string IRFCode { get; set; }
        public string RoleTitle { get; set; }
        public int? DurationMinutes { get; set; }
        public bool CodeSnippetRequired { get; set; }
        public bool IsInterviewerAlignmentRequired { get; set; }
        public bool AITrialEnabled { get; set; }

        /// <summary>
        /// Pre-interview mirror of usp_GetTrialJudgmentContext.IsFirstCompletedInterview, and only when
        /// IRF.AITrialEnabled = 1. Both read fn_InterviewerCompletedInterviewsOnIrf.
        /// </summary>
        public bool IsTrialInterview { get; set; }

        public int CompletedOnChannelBefore { get; set; }
        public int? InterviewerUserId { get; set; }
        public bool TrialOnboardingAcknowledged { get; set; }
        public DateTime? TrialOnboardingAcknowledgedOn { get; set; }
        public string TrialOnboardingPolicyVersion { get; set; }

        /// <summary>Server-side only: the key for the candidate's CandidatePseudonym. Never rendered.</summary>
        public string CandidateEmail { get; set; }
    }

    /// <summary>One of the interviewer's own questions (TechSkillComments) with this interview's code session, if minted.</summary>
    public class InterviewerCodeQuestion
    {
        public int QuestionId { get; set; }
        public string Comment { get; set; }
        public string SkillName { get; set; }
        public bool IsCodingQuestion { get; set; }
        public int? CodingTimeLimitInMinutes { get; set; }
        public Guid? Token { get; set; }
        public int? RunCount { get; set; }
        public string LastRunStatus { get; set; }
        public DateTime? OpenedOn { get; set; }
        public DateTime? SubmittedOn { get; set; }
    }

    /// <summary>A row of InterviewCodeSessions: one code session per interview x question.</summary>
    public class InterviewCodeSession
    {
        public int Id { get; set; }
        public int InterviewId { get; set; }
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public Guid Token { get; set; }
        public string LanguageId { get; set; }
        public string SubmittedSource { get; set; }
        public string LastRunSource { get; set; }
        public string LastRunOutput { get; set; }
        public string LastRunStatus { get; set; }
        public int RunCount { get; set; }
        public int? CodingTimeLimitInMinutes { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? OpenedOn { get; set; }
        public DateTime? SubmittedOn { get; set; }

        /// <summary>Only filled by the by-token lookup, to expire the token with the interview.</summary>
        public DateTime? InterviewDate { get; set; }
        public int? InterviewDurationMinutes { get; set; }

        /// <summary>Only filled by the by-token lookup: the rollout switch on the interview's own channel.</summary>
        public bool IsInterviewerAlignmentRequired { get; set; }

        /// <summary>Minutes between the candidate opening the link and submitting, when both are known.</summary>
        public int? MinutesUsed =>
            OpenedOn.HasValue && SubmittedOn.HasValue && SubmittedOn.Value >= OpenedOn.Value
                ? (int)Math.Ceiling((SubmittedOn.Value - OpenedOn.Value).TotalMinutes)
                : (int?)null;
    }

    /// <summary>
    /// The requirements the alignment page's "Ready to join" card can hold the Zoom link on. Two are the
    /// rubric and sample-question panels; the rest are the existing InterviewCheckshow checklist items, by
    /// their element ids, so the gate is built from the boxes that already exist rather than new labels.
    /// Which of them render depends on the channel (four to nine checklist items), and the total is derived
    /// from what rendered. The override endpoint stores only keys from this list.
    /// </summary>
    public static class JoinGateRequirementKeys
    {
        public const string Rubric = "rubric";
        public const string Questions = "questions";

        /// <summary>Before you join: the page watched it happen.</summary>
        public const string PreviousIssues = "checklist_9";
        public const string QualitySla = "checklist_6";
        public const string FlowVideo = "checklist_7";
        public const string FlowDocument = "checklist_8";

        /// <summary>During the interview: the interviewer confirms they will do it.</summary>
        public const string OwnQuestions = "checklist_5";
        public const string IdVerification = "checklist_2";
        public const string JobConsent = "checklist_1";
        public const string CandidateCode = "checklist_3";
        public const string ProxyAlert = "checklist_4";

        public static readonly IReadOnlyList<string> All = new[]
        {
            Rubric, Questions, PreviousIssues, QualitySla, FlowVideo, FlowDocument,
            OwnQuestions, IdVerification, JobConsent, CandidateCode, ProxyAlert
        };
    }

    /// <summary>Result of usp_CreateInterviewCodeSession.</summary>
    public class CreateInterviewCodeSessionResult
    {
        /// <summary>ok, not_interviewer, not_owner, empty_question.</summary>
        public string Status { get; set; }
        public InterviewCodeSession Session { get; set; }
    }

    /// <summary>
    /// Everything the interview alignment page (InterviewAlignment.cshtml) renders beyond the Meeting: the
    /// join gate, the candidate card (coded identity only), the trial banner and one-time onboarding modal,
    /// and the interviewer's own questions with their code sessions. Built only for channels with
    /// IRF.IsInterviewerAlignmentRequired on; when it cannot be built the interviewer gets today's
    /// InterviewDetails.cshtml instead.
    /// </summary>
    public class InterviewAlignmentViewModel
    {
        public int InterviewId { get; set; }
        public int IRFIntId { get; set; }
        public string IRFCode { get; set; }

        /// <summary>CandidatePseudonym nickname, e.g. AdLo1234. Never the real name or email.</summary>
        public string CandidateCodedName { get; set; }
        public string RoleTitle { get; set; }
        public int? DurationMinutes { get; set; }
        public bool CodeSnippetRequired { get; set; }

        public bool IsTrialInterview { get; set; }
        public bool ShowTrialOnboarding { get; set; }
        public string TrialPolicyUrl { get; set; }
        public string TrialPolicyVersion { get; set; }
        public int CompletedOnChannelBefore { get; set; }

        /// <summary>True when the pre-interview screening report (the "candidate pre-check") exists.</summary>
        public bool HasCandidatePreCheck { get; set; }

        public bool CodeRunnerEnabled { get; set; }
        public int DefaultCodingTimeLimitInMinutes { get; set; }
        public int MaxQuestionLength { get; set; }
        public IList<string> Skills { get; set; } = new List<string>();
        public IList<InterviewerCodeQuestion> Questions { get; set; } = new List<InterviewerCodeQuestion>();
        public IList<InterviewCodeSession> Sessions { get; set; } = new List<InterviewCodeSession>();
    }

    /// <summary>The candidate's code session page, opened by token without signing in.</summary>
    public class CodeSessionPageViewModel
    {
        public Guid Token { get; set; }
        public string QuestionText { get; set; }
        public int? CodingTimeLimitInMinutes { get; set; }
        public DateTime? OpenedOn { get; set; }
        public DateTime ServerNow { get; set; }
        public string LanguageId { get; set; }
        public string Source { get; set; }
        public string LastRunOutput { get; set; }
        public DateTime? SubmittedOn { get; set; }
        public bool RunEnabled { get; set; }
        public bool IsExpired { get; set; }
        public IList<CodeLanguageOption> Languages { get; set; } = new List<CodeLanguageOption>();
    }

    public class CreateCodeSessionRequestVM
    {
        public int InterviewId { get; set; }

        /// <summary>0 for a question typed into the live field; otherwise one of the interviewer's own TechSkillComments ids.</summary>
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string SkillName { get; set; }
        public int? TimeLimitInMinutes { get; set; }
    }

    public class CodeSessionRunRequestVM
    {
        public Guid Token { get; set; }
        public string Language { get; set; }
        public string Code { get; set; }
        public string Stdin { get; set; }
    }

    public class CodeSessionSubmitRequestVM
    {
        public Guid Token { get; set; }
        public string Language { get; set; }
        public string Code { get; set; }
    }
}
