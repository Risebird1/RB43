using System;

namespace Risebird.Entities.ViewModel
{
    public sealed class AutoQcFlagOptions
    {
        public string Source { get; set; } = DmIvAutoQcAuditConstants.SourceForwardTrigger;
        public string Tag { get; set; } = DmIvAutoQcAuditConstants.FlagTag;
        public Guid? BackfillBatchId { get; set; }
        public string ExtraMetadata { get; set; }
        /// <summary>Skip if bad-quality flag already set on this trans row (backfill).</summary>
        public bool SkipIfAlreadyFlagged { get; set; }
        public string BadgeVerdict { get; set; }
        public int? BadgeTriggeringInterviewId { get; set; }
    }
}
