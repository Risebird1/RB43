using System;

namespace Risebird.Entities.Data
{
    public class TransInterviewerAuditLog
    {
        public int Id { get; set; }
        public int TransInterviewerId { get; set; }
        public string EventType { get; set; }
        public string FromValue { get; set; }
        public string ToValue { get; set; }
        public string ReasonCategory { get; set; }
        public int? TriggeringInterviewId { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string AuditTag { get; set; }
        public string AuditSource { get; set; }
        public Guid? BackfillBatchId { get; set; }
        public string ExtraMetadata { get; set; }
    }

    public class TransInterviewerInterviewContext
    {
        public int TransInterviewerId { get; set; }
        public int UserId { get; set; }
        public int IrfId { get; set; }
        public bool IsActive { get; set; }
        public string InterviewerEmail { get; set; }
        public string InterviewerCodedId { get; set; }
        public bool RbVetAlreadyNo { get; set; }
        public string ClientAction { get; set; }
        public bool IsRbVetYes { get; set; }
        public string AutoQcVerdict { get; set; }
    }

    public class TransInterviewerInactiveAuditSummary
    {
        public int TransInterviewerId { get; set; }
        public bool InactiveAuditSummaryLoaded { get; set; }
        public string InactiveAuditEventType { get; set; }
        public string InactiveAuditReasonDetail { get; set; }
        public int? InactiveAuditTriggeringInterviewId { get; set; }
        public DateTime? InactiveAuditChangedAt { get; set; }
    }
}
