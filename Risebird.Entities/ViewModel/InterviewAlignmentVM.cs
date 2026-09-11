using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// The pre-meeting briefing an interviewer opens before conducting an interview.
    ///
    /// Property names match the column aliases in usp_GetInterviewAlignmentDetails exactly,
    /// so Dapper maps them by name with no custom mapper. Renaming a property here means
    /// renaming the alias there.
    ///
    /// The collections are initialised in the constructor so the view can call .Any() on the
    /// unauthorised path, where nothing is ever populated.
    /// </summary>
    public class InterviewAlignmentVM
    {
        public InterviewAlignmentVM()
        {
            RubricSkills = new List<InterviewAlignmentRubricSkill>();
            SampleQuestions = new List<InterviewAlignmentSampleQuestion>();
            ClientNotes = new List<InterviewAlignmentClientNote>();
            FlaggedInterviews = new List<InterviewAlignmentFlaggedInterview>();
            PerformanceSummary = new InterviewAlignmentPerformanceSummary();
        }

        /// <summary>
        /// Set in the repository from whether result set 1 returned a row. The procedure only
        /// returns that row to the interviewer assigned to the interview, so an empty set 1 is
        /// the authorisation signal — the C# layer never re-checks the assignment itself.
        /// </summary>
        public bool IsAssignedInterviewer { get; set; }

        public InterviewAlignmentContext Context { get; set; }

        public List<InterviewAlignmentRubricSkill> RubricSkills { get; set; }
        public List<InterviewAlignmentSampleQuestion> SampleQuestions { get; set; }
        public List<InterviewAlignmentClientNote> ClientNotes { get; set; }
        public List<InterviewAlignmentFlaggedInterview> FlaggedInterviews { get; set; }
        public InterviewAlignmentPerformanceSummary PerformanceSummary { get; set; }

        // Applied by the controller from configuration, not by the procedure.
        public string TrialPolicyUrl { get; set; }
        public string TrialPolicyVersion { get; set; }
        public int SampleQuestionsVisibleCap { get; set; }

        /// <summary>
        /// Both conditions are required. Enabling the trial flag on an IRF does not
        /// retroactively make a veteran interviewer's next interview on it a trial.
        /// </summary>
        public bool IsTrialInterview
            => Context != null && Context.AiTrialEnabled && Context.IsFirstInterviewOnIrf;

        /// <summary>
        /// The one-time modal: a trial interview for an interviewer who has never
        /// acknowledged the trial terms.
        /// </summary>
        public bool ShowTrialOnboardingModal
            => IsTrialInterview && Context != null && !Context.TrialOnboardingAcknowledged;
    }

    /// <summary>Result set 1 — interview and candidate context. Zero rows when unauthorised.</summary>
    public class InterviewAlignmentContext
    {
        public int InterviewId { get; set; }
        public int IrfIntId { get; set; }
        public string IRFCode { get; set; }

        /// <summary>Derived, never the candidate's real name — see §9.4 of the handoff.</summary>
        public string CandidateCodedName { get; set; }

        public string RoleTitle { get; set; }
        public string ClientName { get; set; }

        /// <summary>Coded or encoded resume link only. The raw ResumeLink is never selected.</summary>
        public string CandidateCodedResumeLink { get; set; }

        public int? ExpectedDurationMinutes { get; set; }

        /// <summary>Stored in IST, not UTC.</summary>
        public DateTime InterviewDate { get; set; }

        public string CandidateTimeZoneLabel { get; set; }

        /// <summary>Browser convention: positive west of UTC. IST is -330.</summary>
        public int? CandidateTimeZoneOffsetMinutes { get; set; }

        public bool AiTrialEnabled { get; set; }
        public bool IsFirstInterviewOnIrf { get; set; }
        public bool TrialOnboardingAcknowledged { get; set; }
        public string TrialOnboardingPolicyVersion { get; set; }
        public string InterviewerCodedName { get; set; }
        public bool IsVideoRequired { get; set; }
        public string TechRubricType { get; set; }
    }

    /// <summary>Result set 2 — the rubric the candidate is evaluated against.</summary>
    public class InterviewAlignmentRubricSkill
    {
        public int Id { get; set; }
        public int? SkillId { get; set; }
        public string SkillName { get; set; }
        public int? LevelId { get; set; }

        /// <summary>Not from SQL — filled by MeetingService from LevelTechEnum.</summary>
        public string LevelName { get; set; }

        public int? Weightage { get; set; }
        public bool IsEliminationSkill { get; set; }

        /// <summary>1 = primary stack, 2 = secondary.</summary>
        public int SkillSet { get; set; }

        public string Concepts { get; set; }
        public string NatureOfQuestion { get; set; }
        public int? TimeLimit { get; set; }
    }

    /// <summary>Result set 3 — sample questions for the IRF's skill channel.</summary>
    public class InterviewAlignmentSampleQuestion
    {
        public int Id { get; set; }
        public int? SkillId { get; set; }

        /// <summary>Grouping key in the view. Blank is rendered as "Other".</summary>
        public string SkillName { get; set; }

        public string QuestionText { get; set; }
        public string ExpectedAnswer { get; set; }

        /// <summary>
        /// Aliased from the existing SkillChannelSampleQuestions.AIQuestionVideoLink column
        /// rather than a new VideoLink column — see the note in the procedure.
        /// </summary>
        public string VideoLink { get; set; }

        public string ThumbnailLink { get; set; }
    }

    /// <summary>Result set 4 — client instructions for the IRF, plus any raised against this interview.</summary>
    public class InterviewAlignmentClientNote
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public DateTime? SentAt { get; set; }
        public bool IsInterviewSpecific { get; set; }
    }

    /// <summary>Result set 5 — the interviewer's own past interviews on this IRF that carry a concerning signal.</summary>
    public class InterviewAlignmentFlaggedInterview
    {
        public InterviewAlignmentFlaggedInterview()
        {
            Issues = new List<string>();
        }

        public int InterviewId { get; set; }
        public int IrfIntId { get; set; }
        public DateTime InterviewDate { get; set; }
        public string CandidateCodedName { get; set; }

        /// <summary>0–5. How many independent signals fired.</summary>
        public int SignalCount { get; set; }

        /// <summary>Derived from SignalCount until item 70 ships a real risk signal.</summary>
        public string RiskCategory { get; set; }

        public bool IsSuspiciousQC { get; set; }
        public bool IsRedo { get; set; }
        public bool IsLowCandidateRating { get; set; }
        public bool IsNegativeAIReportVerdict { get; set; }
        public bool IsNegativeAITranscriptVerdict { get; set; }

        public int? CandidateRating { get; set; }
        public int? AIReportQCAnalysisScore { get; set; }
        public int? AITranscriptAnalysisScore { get; set; }

        public string AIReasoning { get; set; }
        public string QCComment { get; set; }

        /// <summary>Not from SQL — built by MeetingService.DescribeConcerningSignals.</summary>
        public List<string> Issues { get; set; }
    }

    /// <summary>
    /// Result set 6 — aggregate over the same population as result set 5, so the two can
    /// never disagree. Always exactly one row, all-zero on the unauthorised path.
    /// </summary>
    public class InterviewAlignmentPerformanceSummary
    {
        public int TotalInterviews { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalSelects { get; set; }
        public int TotalRejects { get; set; }

        /// <summary>Nullable on purpose: the view renders "-" rather than 0 when nothing is scored.</summary>
        public decimal? AverageAIReportScore { get; set; }
        public decimal? AverageAITranscriptScore { get; set; }

        /// <summary>Denominator disclosure for the average above.</summary>
        public int AIReportScoredInterviews { get; set; }

        public int NegativeAIReportVerdicts { get; set; }
        public decimal? AverageCandidateRating { get; set; }
        public int CandidateRatingResponses { get; set; }

        public int SuspiciousCount { get; set; }
        public int RedoCount { get; set; }
        public int LowRatedCount { get; set; }
        public int NegativeAIVerdictCount { get; set; }

        /// <summary>Interviews where two or more signals fired.</summary>
        public int ConcerningRiskCount { get; set; }
    }
}
