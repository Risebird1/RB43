using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// An expert's assignment to audit one AI code interview report. The outcome of the audit is
    /// stamped onto this row on submit so a QC listing can triage severe disagreements without
    /// reading the discrepancy rows.
    /// </summary>
    public class AICodeInterviewExpertAssignmentVM
    {
        public int id { get; set; }
        public string SessionId { get; set; }
        public int InterviewId { get; set; }
        public int IRFIntId { get; set; }
        public string ExpertEmail { get; set; }
        public string AssignedBy { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public bool IsActive { get; set; }
        public bool? WasDiscrepancyFound { get; set; }

        /// <summary>Highest severity across this audit's discrepancy rows. See AICodeInterviewDiscrepancySeverityEnum.</summary>
        public int? MaxSeverity { get; set; }
    }

    /// <summary>
    /// One field level difference between the AI report and the expert copy. These rows are the
    /// durable record of what the expert changed and are what the read view renders from.
    /// </summary>
    public class AICodeInterviewReportDiscrepancyVM
    {
        public int id { get; set; }
        public string SessionId { get; set; }
        public int? AssignmentId { get; set; }
        public int? ExpertReportId { get; set; }

        /// <summary>Name of AICodeInterviewDiscrepancyFieldTypeEnum.</summary>
        public string FieldType { get; set; }

        public int? QuestionId { get; set; }
        public int? DisplayOrder { get; set; }

        /// <summary>Rubric dimension or skill name, for the rubric field types.</summary>
        public string FieldKey { get; set; }

        public string AIValue { get; set; }
        public string ExpertValue { get; set; }
        public bool IsDiscrepancy { get; set; }
        public int Severity { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    /// <summary>
    /// Everything the expert report page and the read only audit view need: the AI report, the
    /// expert copy if one exists, the assignment, and the stored discrepancies.
    /// </summary>
    public class AICodeInterviewAuditReportVM
    {
        public string SessionId { get; set; }
        public int InterviewId { get; set; }
        public int IRFIntId { get; set; }
        public string IRFId { get; set; }
        public string CandidateName { get; set; }
        public string CandidateEmail { get; set; }
        public DateTime InterviewDate { get; set; }
        public string JDTitle { get; set; }
        public string resumeLink { get; set; }

        public AICodeInterviewReportVM AIReport { get; set; }
        public AICodeInterviewReportVM ExpertReport { get; set; }
        public AICodeInterviewExpertAssignmentVM Assignment { get; set; }
        public List<AICodeInterviewReportDiscrepancyVM> Discrepancies { get; set; }

        /// <summary>True once the expert has submitted, which switches the page to its read view.</summary>
        public bool IsSubmitted { get; set; }

        /// <summary>True when the viewer may edit and submit this audit.</summary>
        public bool CanEdit { get; set; }
    }

    /// <summary>
    /// Outcome of submitting an expert audit, including the recomputed discrepancy summary that was
    /// stamped onto the assignment.
    /// </summary>
    public class AICodeInterviewAuditSubmitResultVM
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int ExpertReportId { get; set; }
        public bool WasDiscrepancyFound { get; set; }
        public int MaxSeverity { get; set; }
    }

    /// <summary>
    /// A completed AI code interview surfaced as a QC item, either in the open pool or in the
    /// signed in expert's assigned list.
    /// </summary>
    public class AICodeInterviewQCItemVM
    {
        public string SessionId { get; set; }
        public int InterviewId { get; set; }
        public int irfIntId { get; set; }
        public string IRFId { get; set; }
        public string JDTiltle { get; set; }
        public string candidateName { get; set; }
        public string candidateEmail { get; set; }
        public DateTime interviewDate { get; set; }
        public int TotalQuestions { get; set; }

        public int? AssignmentId { get; set; }
        public string ExpertEmail { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public bool? WasDiscrepancyFound { get; set; }
        public int? MaxSeverity { get; set; }

        public bool IsSubmitted => SubmittedAt.HasValue;
    }
}
