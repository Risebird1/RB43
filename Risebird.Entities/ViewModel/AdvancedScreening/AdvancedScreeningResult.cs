using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel.AdvancedScreening
{
    /// <summary>
    /// The resume scorer's response plus the totals computed in code. Stored as
    /// CandidateAdvancedScreening.ResponseJson. The model never supplies totals, percentages,
    /// verdicts or the authenticity rating - AdvancedScreeningScorer computes them.
    /// </summary>
    public class AdvancedScreeningResult
    {
        // ---- from the scorer
        [JsonProperty("criteria")]
        public List<CriterionScore> Criteria { get; set; } = new List<CriterionScore>();

        [JsonProperty("dealbreakersFailed")]
        public List<DealbreakerFailure> DealbreakersFailed { get; set; } = new List<DealbreakerFailure>();

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("strengths")]
        public List<string> Strengths { get; set; } = new List<string>();

        [JsonProperty("concerns")]
        public List<string> Concerns { get; set; } = new List<string>();

        [JsonProperty("authenticity")]
        public AuthenticityResult Authenticity { get; set; } = new AuthenticityResult();

        [JsonProperty("probeQuestions")]
        public List<string> ProbeQuestions { get; set; } = new List<string>();

        // ---- computed in code
        [JsonProperty("computed")]
        public ComputedTotals Computed { get; set; }
    }

    public class CriterionScore
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("score")]
        public decimal Score { get; set; }

        [JsonProperty("maxScore")]
        public decimal MaxScore { get; set; }

        [JsonProperty("matched")]
        public bool Matched { get; set; }

        [JsonProperty("observation")]
        public string Observation { get; set; }
    }

    public class DealbreakerFailure
    {
        [JsonProperty("dealbreaker")]
        public string Dealbreaker { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }
    }

    public class AuthenticityResult
    {
        [JsonProperty("flags")]
        public List<AuthenticityFlag> Flags { get; set; } = new List<AuthenticityFlag>();
    }

    public class AuthenticityFlag
    {
        /// <summary>CompanyGenuineness, Stability, KpiRealism, ProjectGenuineness, ProjectDuplicate,
        /// JdMirroring, DateInconsistency or PromptInjection.</summary>
        [JsonProperty("check")]
        public string Check { get; set; }

        /// <summary>High, Medium or Low.</summary>
        [JsonProperty("severity")]
        public string Severity { get; set; }

        [JsonProperty("evidence")]
        public string Evidence { get; set; }

        /// <summary>"Resume" when the scorer judged it, "Verified facts" when it rests on a computed fact.</summary>
        [JsonProperty("source")]
        public string Source { get; set; }
    }

    public class ComputedTotals
    {
        [JsonProperty("totalScore")]
        public decimal TotalScore { get; set; }

        [JsonProperty("maxScore")]
        public decimal MaxScore { get; set; }

        [JsonProperty("overallPercentage")]
        public decimal OverallPercentage { get; set; }

        [JsonProperty("verdict")]
        public string Verdict { get; set; }

        [JsonProperty("dealbreakerOverride")]
        public bool DealbreakerOverride { get; set; }

        [JsonProperty("sections")]
        public List<SectionTotal> Sections { get; set; } = new List<SectionTotal>();

        [JsonProperty("authenticityRating")]
        public string AuthenticityRating { get; set; }

        [JsonProperty("authenticityFlagCount")]
        public int AuthenticityFlagCount { get; set; }

        [JsonProperty("rubricVersion")]
        public int RubricVersion { get; set; }

        [JsonProperty("instructionsVersion")]
        public int InstructionsVersion { get; set; }

        [JsonProperty("screenedOn")]
        public DateTime ScreenedOn { get; set; }
    }

    public class SectionTotal
    {
        [JsonProperty("section")]
        public string Section { get; set; }

        [JsonProperty("score")]
        public decimal Score { get; set; }

        /// <summary>The sum of the section's weights in the rubric (not only those scored).</summary>
        [JsonProperty("maxScore")]
        public decimal MaxScore { get; set; }
    }

    public static class ScreeningVerdicts
    {
        public const string StrongFit = "Strong Fit";
        public const string GoodFit = "Good Fit";
        public const string PartialFit = "Partial Fit";
        public const string NotAFit = "Not a Fit";
    }

    public static class AuthenticityRatings
    {
        public const string Clean = "Clean";
        public const string Review = "Review";
        public const string HighRisk = "High risk";
    }

    public static class AdvancedScreeningStatus
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
        public const string Skipped = "Skipped";
    }

    /// <summary>usp_GetCandidateAdvancedScreening: one candidate's result and its context.</summary>
    public class CandidateAdvancedScreeningVM
    {
        public int CandidateId { get; set; }
        public int CandidateMasterId { get; set; }
        public int IRFId { get; set; }
        public string ResumeLink { get; set; }
        public string CandidateEmail { get; set; }
        public string CandidateName { get; set; }
        public string IRFName { get; set; }
        public string JDTiltle { get; set; }
        public bool IsAdvancedResumeScreening { get; set; }
        public int? CurrentRubricVersion { get; set; }
        public int? CurrentInstructionsVersion { get; set; }
        public string RubricStatus { get; set; }

        public bool HasResult { get; set; }
        public string Status { get; set; }
        public int? RubricVersion { get; set; }
        public int? InstructionsVersion { get; set; }
        public decimal? TotalScore { get; set; }
        public decimal? OverallPercentage { get; set; }
        public string Verdict { get; set; }
        public decimal? SkillsScore { get; set; }
        public decimal? WorkScore { get; set; }
        public decimal? EducationScore { get; set; }
        public string AuthenticityRating { get; set; }
        public int? AuthenticityFlagCount { get; set; }
        public string ResponseJson { get; set; }
        public string FactsJson { get; set; }
        public int AttemptCount { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime? ScreenedOn { get; set; }
        public string TriggeredBy { get; set; }

        [JsonIgnore]
        public bool IsStale =>
            (RubricVersion ?? 0) < (CurrentRubricVersion ?? 0)
            || (InstructionsVersion ?? 0) < (CurrentInstructionsVersion ?? 0);
    }

    /// <summary>A row claimed by usp_GetCandidatesForAdvancedScreening.</summary>
    public class AdvancedScreeningWorkItem
    {
        public int CandidateId { get; set; }
        public int IRFId { get; set; }
        public string TriggeredBy { get; set; }
        public int AttemptCount { get; set; }
    }

    /// <summary>The outcome of screening one candidate.</summary>
    public class ScreenCandidateOutcome
    {
        public string Status { get; set; }
        public string Error { get; set; }
        public AdvancedScreeningResult Result { get; set; }
    }
}
