using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class AutoQcRemovalMonitorVm
    {
        public int TodayRemovals { get; set; }
        public int Rolling7DayRemovals { get; set; }
        public int Rolling30DayRemovals { get; set; }
        public int Rolling30DayReactivations { get; set; }
        public decimal ReactivationRatePercent { get; set; }
        public int ForwardTriggerCount { get; set; }
        public int BackfillCount { get; set; }
        public int SuspiciousCount { get; set; }
        public int RedoCount { get; set; }
        public AutoQcBackfillRunSummaryVm LatestBackfill { get; set; }
        public List<AutoQcRemovalMonitorRowVm> Removals { get; set; } = new List<AutoQcRemovalMonitorRowVm>();
        public List<AutoQcRemovalMonitorIrfBreakdownVm> IrfBreakdown { get; set; } = new List<AutoQcRemovalMonitorIrfBreakdownVm>();
        public DateTime? FilterFrom { get; set; }
        public DateTime? FilterTo { get; set; }
        public int? FilterIrfId { get; set; }
        public string FilterSource { get; set; }
        public string FilterVerdict { get; set; }
    }

    public class AutoQcRemovalMonitorRowVm
    {
        public int AuditLogId { get; set; }
        public DateTime RemovedAt { get; set; }
        public string InterviewerCodedId { get; set; }
        public int IrfId { get; set; }
        public string IrfName { get; set; }
        public int? TriggeringInterviewId { get; set; }
        public string Verdict { get; set; }
        public string Source { get; set; }
        public Guid? BackfillBatchId { get; set; }
        public string ExtraMetadata { get; set; }
        public string QcDetailsUrl { get; set; }
        public bool WasReactivated { get; set; }
    }

    public class AutoQcRemovalMonitorIrfBreakdownVm
    {
        public int IrfId { get; set; }
        public string IrfName { get; set; }
        public int Removals { get; set; }
        public int Reactivations { get; set; }
        public int SuspiciousRemovals { get; set; }
        public int RedoRemovals { get; set; }
    }

    public class AutoQcBackfillRunSummaryVm
    {
        public Guid BatchId { get; set; }
        public DateTime ExecutedAtUtc { get; set; }
        public int RecordsRemoved { get; set; }
        public int TriggeringInterviewCount { get; set; }
        public string ExecutedBy { get; set; }
    }

    public class AutoQcBackfillResultVm
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public Guid? BatchId { get; set; }
        public int RecordsRemoved { get; set; }
        public int RecordsSkipped { get; set; }
        public int TriggeringInterviewCount { get; set; }
        public int PreDeploymentCandidateCount { get; set; }
    }

    public class AutoQcBackfillCandidateRow
    {
        public int InterviewId { get; set; }
        public int TransInterviewerId { get; set; }
        public int IrfId { get; set; }
        public string Verdict { get; set; }
        public DateTime? VerdictFinalizedAt { get; set; }
        public bool AlreadyFlagged { get; set; }
        public string InterviewerEmail { get; set; }
        public int UserId { get; set; }
        public string InterviewerCodedId { get; set; }
        public string CurrentAutoQcVerdict { get; set; }
    }

    public class AutoQcPerIrfBackfillPreviewVm
    {
        public int IrfId { get; set; }
        public int VerdictCount { get; set; }
        public int WouldFlagCount { get; set; }
        public int AlreadyFlaggedCount { get; set; }
        public int UnmappedVerdictCount { get; set; }
        public int QcMergedInRangeCount { get; set; }
        public DateTime? OldestVerdictDate { get; set; }
        public DateTime? NewestVerdictDate { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class AutoQcPerIrfBackfillResultVm
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public Guid? RunId { get; set; }
        public int VerdictsScanned { get; set; }
        public int NewFlagsSet { get; set; }
        public int AlreadyFlaggedSkipped { get; set; }
    }

    public class IrfAuditDirectoryRowVm
    {
        public int IrfId { get; set; }
        public string IrfName { get; set; }
        public int TotalCompletedLifetime { get; set; }
        public int CompletedLast90Days { get; set; }
        public int SuspiciousRedoLast90Days { get; set; }
        public int PendingFlagsCount { get; set; }
        public DateTime? LastBackfillAtUtc { get; set; }
    }

    public class IrfAuditInterviewerRiskRowVm
    {
        public int TransInterviewerId { get; set; }
        public int InterviewerId { get; set; }
        /// <summary>UserList.id — use for TrialStats / InterviewerStatsDetails drill-through.</summary>
        public int UserId { get; set; }
        public string InterviewerCodedId { get; set; }
        public string InterviewerEmail { get; set; }
        public int IrfId { get; set; }
        public string IrfName { get; set; }
        /// <summary>Completed interviews in the selected date window.</summary>
        public int TotalCompleted { get; set; }
        /// <summary>Lifetime completed on this IRF (display only — not used in ratios).</summary>
        public int LifetimeCompleted { get; set; }
        public int RiskInterviews { get; set; }
        public int P0Count { get; set; }
        public int P1Count { get; set; }
        public int P2Count { get; set; }
        public int P3Count { get; set; }
        public int P4Count { get; set; }
        public int P5Count { get; set; }
        public decimal? RiskRatio { get; set; }
        public decimal? HighSevRatio { get; set; }
        public int? WorstSeverityInterviewId { get; set; }
        public string Recommendation { get; set; }
        public string QcMergeVerdict { get; set; }
        public int? QcMergeScore { get; set; }
        public int PaymentDeductionTotal { get; set; }
        public string PaymentDeductionReasons { get; set; }
    }

    /// <summary>
    /// Per-interview RiskCategory detail for IRF audit drill-down (Severity + Description + SeverityBreakdown).
    /// </summary>
    public class IrfAuditInterviewerRiskDetailVm
    {
        public int TransInterviewerId { get; set; }
        public int InterviewId { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string InterviewerRaw { get; set; }
        public string Severity { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SeverityBreakdown { get; set; }
    }

    public class IrfAuditRiskBackfillResultVm
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public int Processed { get; set; }
        public int Remaining { get; set; }
        /// <summary>True when the mass-fire circuit breaker aborted before any write.</summary>
        public bool Aborted { get; set; }
        public string AbortedCheckName { get; set; }
        public double? AbortedFireRatePct { get; set; }
    }

    public class IrfAuditRiskRemoveResultVm
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
    }
}
