using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Entities.ViewModel
{
    // ---------- Raw evidence (usp_GetInterviewerExposure) ----------

    public class ExposureProfileVM
    {
        public int InterviewerUserId { get; set; }
        public string InterviewerEmail { get; set; }
        public string InterviewerName { get; set; }
        public string FirstName { get; set; }
        public string Mobile { get; set; }
        public bool IsActive { get; set; }
        public int CoachingSessionsInRange { get; set; }
        public DateTime? LastCoachedAtIst { get; set; }
        public string LastCoachingOutcome { get; set; }
        public string LastCoachEmail { get; set; }
        public DateTime? LastSummaryEmailAtUtc { get; set; }
    }

    /// <summary>RiskCategory row for one interview: SeverityBreakdown is "CheckType:P2,OtherCheck:P3".</summary>
    public class ExposureRiskRowVM
    {
        public int InterviewId { get; set; }
        public int? IrfId { get; set; }
        public string IrfName { get; set; }
        public DateTime? InterviewDateIst { get; set; }
        public string SeverityBreakdown { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
    }

    /// <summary>PaymentSLADetails with a deduction; reason columns hold the QC's reason text (or Yes / No).</summary>
    public class ExposurePaymentRowVM
    {
        public int InterviewId { get; set; }
        public int? IrfId { get; set; }
        public string IrfName { get; set; }
        public DateTime? InterviewDateIst { get; set; }
        public int? PaymentDeduction { get; set; }
        public string Recommendation { get; set; }
        /// <summary>PaymentSLADetails ModifiedBy (or CreatedBy): AISystem / System when the AI review set the deduction.</summary>
        public string LastSavedBy { get; set; }
        public string Inaccuratedecisionmaking { get; set; }
        public string TimeManagement { get; set; }
        public string QuestionsRelevance { get; set; }
        public string ScoreRubricIgnorance { get; set; }
        public string PoorFeedbackComments { get; set; }
        public string CandidateInteractionQuality { get; set; }
        public string LimitedHandoncoding { get; set; }
        public string WrongStatusUpdates { get; set; }
        public string WeakInfrastructure { get; set; }
        public string ProctoringIgnorance { get; set; }
        public string UnethicalPractices { get; set; }
        public string PoorCommunicationSkills { get; set; }
        public string AttendanceJoinCompliance { get; set; }
        public string InterviewerVideo { get; set; }
    }

    public class ExposureTrialRowVM
    {
        public int? TrialFlagId { get; set; }
        public int? IrfId { get; set; }
        public string IrfName { get; set; }
        public int? TriggeringInterviewId { get; set; }
        public DateTime? TriggeringInterviewDate { get; set; }
        public bool? TranscriptQualityPass { get; set; }
        public bool? ReportQualityPass { get; set; }
        public bool? DecisionQualityPass { get; set; }
        public bool? CandidateRatingPass { get; set; }
        public bool? JoinTimePass { get; set; }
        public bool? InterviewDurationPass { get; set; }
        public decimal? TranscriptQualityScore { get; set; }
        public decimal? CandidateRating { get; set; }
        public decimal? JoinTimeMinutes { get; set; }
        public decimal? InterviewDurationMinutes { get; set; }
        public string OverallVerdict { get; set; }
        public string ReviewStatus { get; set; }
    }

    /// <summary>Internal channel standing: Kind = Audit | RedMarker | TotalInterviews.</summary>
    public class ExposureStandingRowVM
    {
        public string Kind { get; set; }
        public int? IrfId { get; set; }
        public string IrfName { get; set; }
        public string Detail { get; set; }
        public bool IsOpen { get; set; }
        public DateTime? AtIst { get; set; }
        public int? InterviewId { get; set; }
    }

    public class InterviewerExposureVM
    {
        public ExposureProfileVM Profile { get; set; }
        public IReadOnlyList<ExposureRiskRowVM> Risks { get; set; } = Array.Empty<ExposureRiskRowVM>();
        public IReadOnlyList<ExposurePaymentRowVM> Payments { get; set; } = Array.Empty<ExposurePaymentRowVM>();
        public IReadOnlyList<ExposureTrialRowVM> Trials { get; set; } = Array.Empty<ExposureTrialRowVM>();
        public IReadOnlyList<ExposureStandingRowVM> Standing { get; set; } = Array.Empty<ExposureStandingRowVM>();
    }

    // ---------- Coaching brief ----------

    public class CoachingEvidenceVM
    {
        public string Text { get; set; }
        /// <summary>Risk check | QC report review | Trial | IRF audit | Red markers | Total Interviews</summary>
        public string Source { get; set; }
        /// <summary>P0..P5 for risk checks; internal, never emailed.</summary>
        public string Severity { get; set; }
        public string IrfName { get; set; }
        /// <summary>yyyy-MM-dd (IST).</summary>
        public string DateIst { get; set; }
        public int? InterviewId { get; set; }
        /// <summary>A flag still awaiting a decision (pending trial, auto-QC verdict, unreviewed red markers, Total Interviews under review).</summary>
        public bool IsOpen { get; set; }
    }

    public class CoachingThemeVM
    {
        public string Key { get; set; }
        public string Title { get; set; }
        /// <summary>The talking point for the call.</summary>
        public string Say { get; set; }
        /// <summary>Channel standing: shown to QC, never emailed.</summary>
        public bool IsInternal { get; set; }
        public string WorstSeverity { get; set; }
        public IReadOnlyList<string> Sources { get; set; } = Array.Empty<string>();
        public IReadOnlyList<CoachingEvidenceVM> Items { get; set; } = Array.Empty<CoachingEvidenceVM>();
        public int ItemCount => Items?.Count ?? 0;
        public int OpenCount => Items?.Count(i => i.IsOpen) ?? 0;
    }

    /// <summary>JSON payload of GET /QCActivity/InterviewerBrief.</summary>
    public class CoachingBriefVM
    {
        public string InterviewerEmail { get; set; }
        public string InterviewerName { get; set; }
        public string FirstName { get; set; }
        public string Mobile { get; set; }
        public bool IsActive { get; set; }
        public string FromIst { get; set; }
        public string ToIst { get; set; }
        public int IssueCount { get; set; }
        public int AreaCount { get; set; }
        public int OpenCount { get; set; }
        public IReadOnlyList<CoachingThemeVM> Themes { get; set; } = Array.Empty<CoachingThemeVM>();
        public int CoachingSessionsInRange { get; set; }
        public DateTime? LastCoachedAtIst { get; set; }
        public string LastCoachingOutcome { get; set; }
        public string LastCoachEmail { get; set; }
        public bool CanEmail { get; set; }
        public DateTime? EmailAvailableAtUtc { get; set; }
        public string Error { get; set; }
        /// <summary>Underlying exception message; filled only when the app runs in Development (cleared by the controller otherwise).</summary>
        public string ErrorDetail { get; set; }
    }

    // ---------- Requests / results ----------

    public class CoachingEmailRequestVM
    {
        public string InterviewerEmail { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public List<string> Themes { get; set; } = new List<string>();
        public string Note { get; set; }
    }

    public class CoachingEmailPreviewVM
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string Html { get; set; }
        public int ThemeCount { get; set; }
        public bool CanSend { get; set; }
        public string Message { get; set; }
    }

    public class LogCoachingSessionRequestVM
    {
        public string InterviewerEmail { get; set; }
        public string Channel { get; set; }
        public string Outcome { get; set; }
        public List<string> Themes { get; set; } = new List<string>();
        public string Agreed { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class CoachingActionResultVM
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public int? SessionId { get; set; }
        public string LinkedCallSid { get; set; }
    }

    /// <summary>Row returned by usp_SaveInterviewerCoachingSession.</summary>
    public class CoachingSaveResultVM
    {
        /// <summary>New session id, -1 = email cooldown, -2 = unknown interviewer.</summary>
        public int Result { get; set; }
        public string LinkedCallSid { get; set; }
    }

    public static class CoachingConstants
    {
        public const int MinAgreedLength = 15;
        public const int MaxAgreedLength = 2000;
        public const int MaxNoteLength = 2000;
        public const int EmailCooldownHours = 12;
        public const int LinkCallWithinHours = 3;
        public const string EmailChannel = "Email";
        public const string EmailOnlyOutcome = "EmailOnly";
        public const string FromName = "Risebird Quality Team";

        public static readonly IReadOnlyDictionary<string, string> Channels =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Call"] = "Call",
                ["WhatsApp"] = "WhatsApp",
                ["Meeting"] = "Video meeting",
                ["Email"] = "Email",
                ["Other"] = "Other"
            };

        public static readonly IReadOnlyDictionary<string, string> Outcomes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["AgreedToChange"] = "Agreed to change",
                ["Disputed"] = "Disputed",
                ["CallBack"] = "Call back",
                ["NoAnswer"] = "No answer",
                ["EmailOnly"] = "Email only"
            };

        public static string CanonicalKey(IReadOnlyDictionary<string, string> map, string value) =>
            string.IsNullOrWhiteSpace(value) ? null : map.Keys.FirstOrDefault(k => string.Equals(k, value.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
