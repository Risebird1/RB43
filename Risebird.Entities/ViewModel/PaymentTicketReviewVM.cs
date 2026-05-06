using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Projection returned by usp_GetOpenPaymentRaisedIssues.
    /// Carries everything the AI assistant needs to decide whether
    /// to close (approve revert) or keep a Payment-type ticket open.
    /// </summary>
    public class PaymentTicketReviewVM
    {
        // ── Ticket ──────────────────────────────────────────────────
        public int TicketId { get; set; }
        public string RequestedBy { get; set; }
        public DateTime RequestedOn { get; set; }

        /// <summary>Interviewer's own comment explaining why the penalty should be reverted.</summary>
        public string InterviewerComment { get; set; }
        public int interviewId { get; set; }

        // ── Interview ────────────────────────────────────────────────
        public DateTime interviewDate { get; set; }
        public string Interviewer { get; set; }
        public DateTime? InterviewerJoinDateTime { get; set; }

        // ── AI evidence blobs (already stored in QcInterviewAIevaluation) ──
        public string RawTranscriptUrl { get; set; }
        public string TranscriptAI { get; set; }
        public string ReportAI { get; set; }
        public string ContradictionAI { get; set; }

        /// <summary>Merged AI QC details containing the full payment deduction breakdown.</summary>
        public string MergedAIQCDetails { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Strongly-typed model for the AI assistant's JSON response
    // (assistant asst_kjUOaWfVxQOymoAN1m29MrR8)
    // ─────────────────────────────────────────────────────────────────────────

    public class PaymentTicketAIVerdict
    {
        [Newtonsoft.Json.JsonProperty("metadata")]
        public AIVerdictMetadata Metadata { get; set; }

        [Newtonsoft.Json.JsonProperty("decision_summary")]
        public AIDecisionSummary DecisionSummary { get; set; }

        [Newtonsoft.Json.JsonProperty("updated_red_flag_table")]
        public AIRedFlagTable UpdatedRedFlagTable { get; set; }

        [Newtonsoft.Json.JsonProperty("revised_penalty_amount")]
        public AIRevisedPenalty RevisedPenaltyAmount { get; set; }

        [Newtonsoft.Json.JsonProperty("ai_agent_problem_analysis")]
        public AIAgentProblemAnalysis AIAgentProblemAnalysis { get; set; }

        [Newtonsoft.Json.JsonProperty("updated_reasoning_comment")]
        public string UpdatedReasoningComment { get; set; }

        // ── Convenience helpers ────────────────────────────────────────────

        /// <summary>
        /// True when the AI's decision warrants closing / resolving the ticket.
        /// Covers "reduce" (partial revert) and "approve" (full revert).
        /// </summary>
        public bool ShouldClose =>
            DecisionSummary?.DecisionInFavorOfInterviewer == true &&
            !string.Equals(DecisionSummary?.FinalDecision, "reject",
                System.StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// True when QC penalty values need to be updated in the DB
        /// (i.e. any decision other than full rejection).
        /// </summary>
        public bool ShouldUpdateQCPenalty =>
            ShouldClose &&
            string.Equals(DecisionSummary?.FinalDecision, "reduce",
                System.StringComparison.OrdinalIgnoreCase);

        /// <summary>Short human-readable status comment for ticket history.</summary>
        public string BuildStatusComment()
        {
            var decision = DecisionSummary?.FinalDecision?.ToUpper() ?? "UNKNOWN";
            var pct = RevisedPenaltyAmount != null
                ? $" (penalty revised: {RevisedPenaltyAmount.OriginalPercentage}% → {RevisedPenaltyAmount.RevisedPercentage}%)"
                : string.Empty;
            return $"[AI Review – {decision}]{pct} {UpdatedReasoningComment}".Trim();
        }
    }

    public class AIVerdictMetadata
    {
        [Newtonsoft.Json.JsonProperty("ticket_id")]
        public string TicketId { get; set; }

        [Newtonsoft.Json.JsonProperty("interviewer_email_id")]
        public string InterviewerEmailId { get; set; }

        [Newtonsoft.Json.JsonProperty("interview_id")]
        public string InterviewId { get; set; }
    }

    public class AIDecisionSummary
    {
        [Newtonsoft.Json.JsonProperty("final_decision")]
        public string FinalDecision { get; set; }         // "reduce" | "approve" | "reject"

        [Newtonsoft.Json.JsonProperty("decision_in_favor_of_interviewer")]
        public bool DecisionInFavorOfInterviewer { get; set; }

        [Newtonsoft.Json.JsonProperty("rebuttal_strength")]
        public string RebuttalStrength { get; set; }

        [Newtonsoft.Json.JsonProperty("evidence_basis")]
        public AIEvidenceBasis EvidenceBasis { get; set; }
    }

    public class AIEvidenceBasis
    {
        [Newtonsoft.Json.JsonProperty("raw_transcript_provided")]
        public bool RawTranscriptProvided { get; set; }

        [Newtonsoft.Json.JsonProperty("raw_report_provided")]
        public bool RawReportProvided { get; set; }

        [Newtonsoft.Json.JsonProperty("evidence_handling_note")]
        public string EvidenceHandlingNote { get; set; }
    }

    public class AIRedFlagTable
    {
        [Newtonsoft.Json.JsonProperty("kept_flags")]
        public System.Collections.Generic.List<AIRedFlag> KeptFlags { get; set; }

        [Newtonsoft.Json.JsonProperty("removed_flags")]
        public System.Collections.Generic.List<AIRedFlag> RemovedFlags { get; set; }
    }

    public class AIRedFlag
    {
        [Newtonsoft.Json.JsonProperty("official_flag_category")]
        public string OfficialFlagCategory { get; set; }

        [Newtonsoft.Json.JsonProperty("official_flag_name")]
        public string OfficialFlagName { get; set; }

        [Newtonsoft.Json.JsonProperty("source_area")]
        public string SourceArea { get; set; }

        [Newtonsoft.Json.JsonProperty("original_penalty_percentage")]
        public int OriginalPenaltyPercentage { get; set; }

        [Newtonsoft.Json.JsonProperty("revised_penalty_percentage")]
        public int RevisedPenaltyPercentage { get; set; }

        [Newtonsoft.Json.JsonProperty("reasoning")]
        public string Reasoning { get; set; }

        [Newtonsoft.Json.JsonProperty("removal_reason")]
        public string RemovalReason { get; set; }
    }

    public class AIRevisedPenalty
    {
        [Newtonsoft.Json.JsonProperty("original_percentage")]
        public int OriginalPercentage { get; set; }

        [Newtonsoft.Json.JsonProperty("revised_percentage")]
        public int RevisedPercentage { get; set; }

        [Newtonsoft.Json.JsonProperty("removed_percentage")]
        public int RemovedPercentage { get; set; }

        [Newtonsoft.Json.JsonProperty("penalty_change_reason")]
        public string PenaltyChangeReason { get; set; }
    }

    public class AIAgentProblemAnalysis
    {
        [Newtonsoft.Json.JsonProperty("decision_in_favor_of_interviewer")]
        public bool DecisionInFavorOfInterviewer { get; set; }

        [Newtonsoft.Json.JsonProperty("problematic_ai_agents")]
        public System.Collections.Generic.List<AIAgentProblem> ProblematicAIAgents { get; set; }
    }

    public class AIAgentProblem
    {
        [Newtonsoft.Json.JsonProperty("ai_agent")]
        public string AIAgent { get; set; }

        [Newtonsoft.Json.JsonProperty("problem_type")]
        public string ProblemType { get; set; }

        [Newtonsoft.Json.JsonProperty("problem_summary")]
        public string ProblemSummary { get; set; }
    }
}
