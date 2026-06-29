using System;

namespace Risebird.Entities.ViewModel
{
    public sealed class DmIvActiveBadQualityBadge
    {
        public string Verdict { get; }
        public string Label { get; }
        public string Tone { get; }

        public DmIvActiveBadQualityBadge(string verdict, string label, string tone)
        {
            Verdict = verdict ?? string.Empty;
            Label = label ?? string.Empty;
            Tone = tone ?? "warning";
        }
    }

    /// <summary>
    /// Bad-quality flag on active interviewer-channel rows (AutoQcVerdict = bad_quality_flag equivalent).
    /// </summary>
    public static class DmIvBadQualityFlagHelper
    {
        public static bool HasBadQualityFlag(TransInterviewer iv) =>
            iv != null && iv.IsActive && !string.IsNullOrWhiteSpace(iv.AutoQcVerdict);

        public static DmIvActiveBadQualityBadge ResolveActiveBadge(TransInterviewer iv)
        {
            if (!HasBadQualityFlag(iv))
                return null;

            var verdict = iv.AutoQcVerdict.Trim().ToLowerInvariant();
            if (verdict == DmIvAutoQcAuditConstants.VerdictSuspicious)
                return new DmIvActiveBadQualityBadge(verdict, "Flagged — Suspicious (Auto QC)", "warning");
            if (verdict == DmIvAutoQcAuditConstants.VerdictRedo)
                return new DmIvActiveBadQualityBadge(verdict, "Flagged — Redo (Auto QC)", "danger");
            return new DmIvActiveBadQualityBadge(verdict, "Flagged — Bad Quality (Auto QC)", "warning");
        }
    }
}
