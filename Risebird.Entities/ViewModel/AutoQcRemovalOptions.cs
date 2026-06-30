using System;

namespace Risebird.Entities.ViewModel
{
    public sealed class AutoQcRemovalOptions
    {
        public string Source { get; set; } = DmIvAutoQcAuditConstants.SourceForwardTrigger;
        public string Tag { get; set; } = DmIvAutoQcAuditConstants.Tag;
        public Guid? BackfillBatchId { get; set; }
        /// <summary>JSON metadata (e.g. all triggering interviews for backfill).</summary>
        public string ExtraMetadata { get; set; }
        /// <summary>When true, skip if RB Vet is already No (backfill only).</summary>
        public bool SkipIfAlreadyInactive { get; set; }
        /// <summary>Verdict to store on TransInterviewer when backfill has multiple triggers.</summary>
        public string BadgeVerdict { get; set; }
        public int? BadgeTriggeringInterviewId { get; set; }
    }
}
