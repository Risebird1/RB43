using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// One build + test run in a throwaway container. Used for validation (starter and reference
    /// states) and for grading a candidate's final workspace. Hidden tests travel in Files here —
    /// a container is disposable; a candidate VM never receives them.
    /// </summary>
    public class CodeProjectRunRequest
    {
        public string StackImageCode { get; set; }
        public List<CodeProjectFile> Files { get; set; } = new List<CodeProjectFile>();
        public string BuildCommand { get; set; }
        public string TestCommand { get; set; }
        public int TimeoutSeconds { get; set; } = 120;
    }

    public class CodeProjectTestResult
    {
        public string Name { get; set; }
        public bool Passed { get; set; }
        /// <summary>Failure message, e.g. "expected 400, was 200".</summary>
        public string Message { get; set; }
    }

    public class CodeProjectRunResult
    {
        /// <summary>False when the runner itself could not be reached or crashed — not a build failure.</summary>
        public bool Completed { get; set; }
        public string Error { get; set; }
        public int BuildExitCode { get; set; }
        public string BuildOutput { get; set; }
        public int? TestExitCode { get; set; }
        public string TestOutput { get; set; }
        public List<CodeProjectTestResult> Tests { get; set; } = new List<CodeProjectTestResult>();
        /// <summary>Wall-clock seconds for build + test together.</summary>
        public double WallSeconds { get; set; }
    }

    /// <summary>
    /// Asks the provisioner for a candidate machine. Deliberately has no field for the secrets
    /// blob: hidden tests and the reference solution must never reach the VM.
    /// </summary>
    public class CodeProjectProvisionRequest
    {
        public string SessionId { get; set; }
        public int QuestionId { get; set; }
        public string StackImageCode { get; set; }
        public string GalleryImageId { get; set; }
        public string OsType { get; set; }
        /// <summary>Short-lived read-only SAS url of the candidate-visible project.zip.</summary>
        public string ProjectZipUrl { get; set; }
        public string WorkspacePath { get; set; } = @"C:\workspace";
        public List<string> ReadOnlyPaths { get; set; } = new List<string>();
    }

    public class CodeProjectProvisionResult
    {
        public bool Accepted { get; set; }
        public string Handle { get; set; }
        public string Error { get; set; }
    }

    public class CodeProjectEnvironmentStatus
    {
        /// <summary>provisioning | ready | failed</summary>
        public string State { get; set; }
        public string ConnectUrl { get; set; }
        public int? VMPropertyId { get; set; }
        /// <summary>Progress line shown while connecting, e.g. "Restoring project files…".</summary>
        public string Message { get; set; }
        /// <summary>
        /// Sign-in for the Bastion shareable link, which carries no credentials of its own. Only
        /// returned once the machine is ready; the web app shows it to the candidate at handover.
        /// </summary>
        public string Username { get; set; }
        public string Password { get; set; }
        public List<CodeProjectIntegrityFlag> IntegrityFlags { get; set; } = new List<CodeProjectIntegrityFlag>();
    }

    public class CodeProjectCommit
    {
        public string Sha { get; set; }
        public string Message { get; set; }
        public DateTime? AtUtc { get; set; }
        public int? FilesChanged { get; set; }
        public int? Insertions { get; set; }
        public int? Deletions { get; set; }
    }

    /// <summary>The candidate's workspace at their last commit, pulled off the VM for grading.</summary>
    public class CodeProjectWorkspaceSnapshot
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public List<CodeProjectFile> Files { get; set; } = new List<CodeProjectFile>();
        /// <summary>Unified diff of baseline..last commit.</summary>
        public string Diff { get; set; }
        public string BaselineSha { get; set; }
        public string HeadSha { get; set; }
        public List<CodeProjectCommit> Commits { get; set; } = new List<CodeProjectCommit>();
        /// <summary>From the Bastion session monitor, for the reviewer.</summary>
        public List<CodeProjectIntegrityFlag> IntegrityFlags { get; set; } = new List<CodeProjectIntegrityFlag>();
    }

    /// <summary>What ResultsJson holds for a graded code project.</summary>
    public class CodeProjectGradeResult
    {
        public List<CodeProjectRequirementResult> Requirements { get; set; } = new List<CodeProjectRequirementResult>();
        public List<CodeProjectTestResult> Tests { get; set; } = new List<CodeProjectTestResult>();
        /// <summary>Weighted requirement score, 0–100.</summary>
        public decimal CorrectnessScore { get; set; }
        public bool BuildSucceeded { get; set; }
        public string BuildOutput { get; set; }
        public List<CodeProjectCommit> Commits { get; set; } = new List<CodeProjectCommit>();
        public string BaselineSha { get; set; }
        public string HeadSha { get; set; }
        public string Error { get; set; }
        /// <summary>
        /// True when the platform, not the candidate, prevented a fair attempt (the machine never started,
        /// the workspace could not be read, the runner failed). The question is then left out of scoring.
        /// </summary>
        public bool Excluded { get; set; }
        public string ExclusionReason { get; set; }
        public List<CodeProjectIntegrityFlag> IntegrityFlags { get; set; } = new List<CodeProjectIntegrityFlag>();
    }
}
