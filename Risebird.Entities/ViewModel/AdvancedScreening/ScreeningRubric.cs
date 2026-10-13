using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Entities.ViewModel.AdvancedScreening
{
    /// <summary>
    /// The rubric JSON: the generator's output, the format stored in IRFScreeningRubric.RubricJson
    /// and the scorer's input. Property names are the JSON contract with the stored prompts.
    /// </summary>
    public class ScreeningRubric
    {
        [JsonProperty("jdTitle")]
        public string JdTitle { get; set; }

        [JsonProperty("criteria")]
        public List<RubricCriterion> Criteria { get; set; } = new List<RubricCriterion>();

        [JsonProperty("dealbreakers")]
        public List<string> Dealbreakers { get; set; } = new List<string>();

        [JsonProperty("totalWeight")]
        public int TotalWeight { get; set; }
    }

    public class RubricCriterion
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("section")]
        public string Section { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        /// <summary>Plain English: what a strong match looks like for this role.</summary>
        [JsonProperty("expected")]
        public string Expected { get; set; }

        /// <summary>Whole number; 0 switches the criterion off.</summary>
        [JsonProperty("weight")]
        public int Weight { get; set; }
    }

    /// <summary>usp_QueueAdvancedScreening's scope, named as the Screen All dialog presents it.</summary>
    public enum ScreenAllScope
    {
        /// <summary>Not yet screened, plus results scored on an older rubric or instructions (the default).</summary>
        UnscreenedAndStale = 0,

        /// <summary>No completed result yet: never screened, failed or skipped.</summary>
        Unscreened = 1,

        /// <summary>Everyone.</summary>
        All = 2
    }

    /// <summary>usp_GetIRFScreeningRubric: an IRF's rubric row with the IRF inputs it is built from.</summary>
    public class IRFScreeningRubricVM
    {
        public int IRFId { get; set; }
        public string IRFName { get; set; }
        public string JDTiltle { get; set; }
        public string JDLink { get; set; }
        public string IdealCandidateProfile { get; set; }
        public bool IsAdvancedResumeScreening { get; set; }
        public int? ClientId { get; set; }

        public bool HasRubric { get; set; }
        public int? RubricId { get; set; }
        public string JDText { get; set; }
        public string Instructions { get; set; }
        public int InstructionsVersion { get; set; }
        public string InstructionsUpdatedBy { get; set; }
        public DateTime? InstructionsUpdatedOn { get; set; }
        public bool ApplyInstructionsToRubric { get; set; } = true;
        public bool ApplyInstructionsToScoring { get; set; } = true;
        public string RubricJson { get; set; }
        public int RubricVersion { get; set; }

        /// <summary>Draft, Generating, Generated, Approved or Failed.</summary>
        public string Status { get; set; }
        public int? TotalWeight { get; set; }
        public string LastError { get; set; }
        public DateTime? GeneratedOn { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }

        /// <summary>RubricJson parsed; null when there is none or it does not parse.</summary>
        [JsonIgnore]
        public ScreeningRubric Rubric { get; set; }

        [JsonIgnore]
        public bool IsApproved => string.Equals(Status, RubricStatus.Approved, StringComparison.OrdinalIgnoreCase);
    }

    public static class RubricStatus
    {
        public const string Draft = "Draft";
        public const string Generating = "Generating";
        public const string Generated = "Generated";
        public const string Approved = "Approved";
        public const string Failed = "Failed";
    }

    /// <summary>usp_GetAdvancedScreeningCounts.</summary>
    public class AdvancedScreeningCounts
    {
        public int TotalWithResume { get; set; }
        public int Unscreened { get; set; }
        public int Stale { get; set; }
        public int StaleInstructions { get; set; }
        public int StaleRubric { get; set; }
        public int Screened { get; set; }
        public int Pending { get; set; }
        public int Processing { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public int RubricVersion { get; set; }
        public int InstructionsVersion { get; set; }

        /// <summary>How many a Screen All with this scope would queue.</summary>
        public int CountFor(ScreenAllScope scope)
        {
            switch (scope)
            {
                case ScreenAllScope.Unscreened: return Unscreened;
                case ScreenAllScope.All: return Math.Max(0, TotalWithResume - Pending - Processing);
                default: return Unscreened + Stale;
            }
        }
    }

    /// <summary>The result of saving instructions: the new version and how many results it made stale.</summary>
    public class SaveInstructionsResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public int InstructionsVersion { get; set; }
        public int StaleCount { get; set; }
    }

    /// <summary>
    /// usp_SaveIRFScreeningRubric's parameters. Every part is optional: a null leaves its column
    /// unchanged, and SaveInstructions decides whether the instruction fields are written at all.
    /// </summary>
    public class RubricSaveRequest
    {
        public int IRFId { get; set; }
        public string User { get; set; }
        public bool SaveInstructions { get; set; }
        public string Instructions { get; set; }
        public bool? ApplyInstructionsToRubric { get; set; }
        public bool? ApplyInstructionsToScoring { get; set; }
        public string JDText { get; set; }
        public string RubricJson { get; set; }
        public int? TotalWeight { get; set; }
        public string Status { get; set; }
        public string LastError { get; set; }
        public bool Approve { get; set; }
    }

    /// <summary>usp_SaveIRFScreeningRubric's result: the versions and status after the save.</summary>
    public class RubricSaveResult
    {
        public int RubricVersion { get; set; }
        public int InstructionsVersion { get; set; }
        public string Status { get; set; }
    }

    /// <summary>usp_SaveCandidateAdvancedScreening's parameters.</summary>
    public class CandidateAdvancedScreeningSave
    {
        public int CandidateId { get; set; }
        public int IRFId { get; set; }
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
        public string ErrorMessage { get; set; }
        public string TriggeredBy { get; set; }
    }

    /// <summary>What the rubric editor posts. Dealbreakers arrive one per line.</summary>
    public class ScreeningRubricForm
    {
        public int IRFId { get; set; }
        public string JdTitle { get; set; }
        public string JDText { get; set; }
        public string Dealbreakers { get; set; }
        public List<RubricCriterion> Criteria { get; set; } = new List<RubricCriterion>();
        public bool Approve { get; set; }

        public ScreeningRubric ToRubric()
        {
            return new ScreeningRubric
            {
                JdTitle = JdTitle,
                Criteria = Criteria ?? new List<RubricCriterion>(),
                Dealbreakers = (Dealbreakers ?? string.Empty)
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => d.Trim().TrimStart('-', '*', '•').Trim())
                    .Where(d => d.Length > 0)
                    .ToList()
            };
        }
    }

    /// <summary>The Screen All Resumes dialog: counts, the current instructions and the scope choices.</summary>
    public class ScreenAllDialogVM
    {
        public int IRFId { get; set; }
        public string IRFName { get; set; }
        public bool IsAdvancedResumeScreening { get; set; }
        public bool IsRubricApproved { get; set; }
        public int RubricVersion { get; set; }
        public int InstructionsVersion { get; set; }
        public string Instructions { get; set; }
        public bool ApplyInstructionsToScoring { get; set; }
        public AdvancedScreeningCounts Counts { get; set; } = new AdvancedScreeningCounts();
    }

    /// <summary>A save or generate outcome reported back to the rubric page.</summary>
    public class RubricOperationResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public IRFScreeningRubricVM Rubric { get; set; }
    }
}
