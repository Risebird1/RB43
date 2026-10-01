using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>A pre-built VM image a code project question may run on (StackImages row).</summary>
    public class StackImage
    {
        public int id { get; set; }
        public string Code { get; set; }
        public string DisplayName { get; set; }
        public string Toolchain { get; set; }
        public string GalleryImageId { get; set; }
        public string OsType { get; set; }
        public bool IsActive { get; set; }
    }

    public static class CodeProjectValidationStatus
    {
        public const string Pending = "pending";
        public const string Validating = "validating";
        public const string Passed = "passed";
        public const string Failed = "failed";
    }

    /// <summary>The project delta for a code project question (CodeProjectDetails row, 1:1 with the question).</summary>
    public class CodeProjectDetails
    {
        public int QuestionId { get; set; }
        public string Language { get; set; }
        public string Framework { get; set; }
        public string StackImageCode { get; set; }
        public string ProjectBlobUri { get; set; }
        public string SecretsBlobUri { get; set; }
        public string EntryPointFile { get; set; }
        public string ReadOnlyPathsJson { get; set; }
        public string BuildCommand { get; set; }
        public string RunCommand { get; set; }
        public string TestCommand { get; set; }
        public bool? HasWebserver { get; set; }
        public string PortsJson { get; set; }
        public string RequirementsJson { get; set; }
        public int? EstimatedMinutes { get; set; }
        public string ValidationStatus { get; set; }
        public string ValidationJson { get; set; }
        public DateTime? ValidatedAtUtc { get; set; }
        public int? ApprovedBySmeId { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }

        // Joined from SkillChannelSampleQuestions for display.
        public int IRFId { get; set; }
        public string SkillName { get; set; }
        public string Comment { get; set; }
        public string QuestionGist { get; set; }
        public int? CodingTimeLimitInMinutes { get; set; }
        /// <summary>Sanitized tier 1/2 payload (no file contents, no tier 3) — see CodeProjectQuestionHelper.BuildSanitizedPayload.</summary>
        public string GeneratedPayload { get; set; }
    }

    public class CodeProjectFile
    {
        public string Path { get; set; }
        public string Content { get; set; }
    }

    public class CodeProjectRequirement
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string TestName { get; set; }
        public decimal Weight { get; set; }
    }

    public class CodeProjectSeededDefect
    {
        public string Id { get; set; }
        public string File { get; set; }
        public List<string> Fails { get; set; } = new List<string>();
        public string Explanation { get; set; }
    }

    /// <summary>
    /// The generator's response contract, all three tiers. Tier 3 (hidden tests, reference solution,
    /// seeded defects) is server-side only and must never be written to a candidate machine.
    /// </summary>
    public class CodeProjectGeneration
    {
        // Tier 1 — shared, maps onto existing SkillChannelSampleQuestions columns
        public string SkillOrCompetency { get; set; }
        public List<string> SecondarySkills { get; set; } = new List<string>();
        public string QuestionText { get; set; }
        public string QuestionGist { get; set; }
        public string ExpectedAnswer { get; set; }
        public int TimeLimitMinutes { get; set; }
        public string Language { get; set; }

        // Tier 2 — the project delta, CodeProjectDetails
        public string Framework { get; set; }
        public string StackImageCode { get; set; }
        public List<CodeProjectFile> ProjectFiles { get; set; } = new List<CodeProjectFile>();
        public string EntryPointFile { get; set; }
        public List<string> EditableFiles { get; set; } = new List<string>();
        public List<string> ReadOnlyPaths { get; set; } = new List<string>();
        public string BuildCommand { get; set; }
        public string RunCommand { get; set; }
        public string TestCommand { get; set; }
        public bool HasWebserver { get; set; }
        public List<int> Ports { get; set; } = new List<int>();
        public List<CodeProjectRequirement> Requirements { get; set; } = new List<CodeProjectRequirement>();
        public int EstimatedMinutes { get; set; }

        // Tier 3 — server-side only
        public List<CodeProjectFile> HiddenTests { get; set; } = new List<CodeProjectFile>();
        public List<CodeProjectFile> ReferenceSolution { get; set; } = new List<CodeProjectFile>();
        public List<CodeProjectSeededDefect> SeededDefects { get; set; } = new List<CodeProjectSeededDefect>();
    }

    /// <summary>One validation check, as surfaced to the author in the generation pipeline.</summary>
    public class CodeProjectValidationStep
    {
        public string Key { get; set; }
        public string Title { get; set; }
        /// <summary>pending | run | ok | failed</summary>
        public string State { get; set; }
        public string Detail { get; set; }
        public double? Seconds { get; set; }
    }

    public class CodeProjectValidationReport
    {
        public List<CodeProjectValidationStep> Steps { get; set; } = new List<CodeProjectValidationStep>();
        public string FailureReason { get; set; }
    }

    public static class CodeProjectEnvironmentState
    {
        public const string Requested = "requested";
        public const string Provisioning = "provisioning";
        public const string Ready = "ready";
        public const string HandedOver = "handedover";
        public const string Submitted = "submitted";
        public const string Failed = "failed";
        public const string Released = "released";
    }

    /// <summary>One candidate machine for one (session, question) — CodeProjectEnvironments row.</summary>
    public class CodeProjectEnvironment
    {
        public int id { get; set; }
        public string SessionId { get; set; }
        public int QuestionId { get; set; }
        public string StackImageCode { get; set; }
        public string State { get; set; }
        public string EnvironmentHandle { get; set; }
        public string ConnectUrl { get; set; }
        public int? VMPropertyId { get; set; }
        public string Message { get; set; }
        public DateTime RequestedAtUtc { get; set; }
        public DateTime? ReadyAtUtc { get; set; }
        /// <summary>The candidate's clock starts here, not at provisioning.</summary>
        public DateTime? HandedOverAtUtc { get; set; }
        public DateTime? SubmittedAtUtc { get; set; }
    }

    public class CodeProjectStatusRow
    {
        public int QuestionId { get; set; }
        public string ValidationStatus { get; set; }
        public string StackImageCode { get; set; }
        public int? EstimatedMinutes { get; set; }
    }

    /// <summary>CodingAssessment row for a code project question.</summary>
    public class CodeProjectAssessment
    {
        public int id { get; set; }
        public string SessionId { get; set; }
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string CodeLanguage { get; set; }
        /// <summary>The candidate's diff against the baseline commit.</summary>
        public string SubmittedCode { get; set; }
        public int? TimeLimitInMinutes { get; set; }
        public DateTime? TimeStarted { get; set; }
        public DateTime? TimeCompleted { get; set; }
        public decimal? TimeTakenInSeconds { get; set; }
        public bool? IsTimeExpired { get; set; }
        public int? VMPropertyId { get; set; }
        public int? RequirementsPassed { get; set; }
        public int? RequirementsTotal { get; set; }
        /// <summary>Serialized <see cref="CodeProjectRequirementResult"/> list plus commit timeline.</summary>
        public string ResultsJson { get; set; }
        public string CreatedBy { get; set; }
    }

    /// <summary>Per-requirement outcome of grading a candidate's final commit against the hidden tests.</summary>
    public class CodeProjectRequirementResult
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string TestName { get; set; }
        public decimal Weight { get; set; }
        public bool Passed { get; set; }
        public string Message { get; set; }
    }
}
