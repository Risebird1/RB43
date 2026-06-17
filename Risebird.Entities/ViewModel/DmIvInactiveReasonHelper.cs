using System;

namespace Risebird.Entities.ViewModel
{
    public enum DmIvInactiveReasonCategory
    {
        PendingRbReview,
        RemovedByRb,
        RemovedByClient,
        Unknown
    }

    public sealed class DmIvInactiveReasonResult
    {
        public DmIvInactiveReasonCategory Category { get; }
        public string Label { get; }
        public string Tone { get; }

        public DmIvInactiveReasonResult(DmIvInactiveReasonCategory category, string label, string tone)
        {
            Category = category;
            Label = label ?? string.Empty;
            Tone = tone ?? "unknown";
        }

        public static DmIvInactiveReasonResult ForCategory(DmIvInactiveReasonCategory category, string reasonDetail = null)
        {
            var suffix = string.IsNullOrWhiteSpace(reasonDetail) ? string.Empty : " — " + reasonDetail.Trim();
            switch (category)
            {
                case DmIvInactiveReasonCategory.PendingRbReview:
                    return new DmIvInactiveReasonResult(category, "Pending RB Review", "info");
                case DmIvInactiveReasonCategory.RemovedByRb:
                    return new DmIvInactiveReasonResult(category, "Removed by RB" + suffix, "warning");
                case DmIvInactiveReasonCategory.RemovedByClient:
                    return new DmIvInactiveReasonResult(category, "Removed by Client" + suffix, "warning");
                default:
                    return new DmIvInactiveReasonResult(DmIvInactiveReasonCategory.Unknown, "Inactive — reason unknown", "unknown");
            }
        }
    }

    /// <summary>
    /// Derives inactive-tab badge labels from interviewer-channel state.
    /// Prefer TransInterviewer vet/active audit history when available (not yet in codebase — see DmV4_LockStateChange_Recovery.sql).
    /// Until then, uses ActivatedAt + vet flags; ambiguous legacy rows return Unknown (never fabricated).
    /// </summary>
    public static class DmIvInactiveReasonHelper
    {
        public static bool WasEverActivatedOnChannel(TransInterviewer iv)
        {
            if (iv?.ActivatedAt == null) return false;
            return iv.ActivatedAt.Value.Year >= 1900;
        }

        public static DmIvInactiveReasonResult Resolve(TransInterviewer iv, bool channelLocked, string reasonDetail = null)
        {
            if (iv == null || iv.IsActive)
                return null;

            if (TryResolveFromAuditLog(iv, reasonDetail, out var fromAudit))
                return fromAudit;

            return ResolveFromCurrentState(iv, channelLocked, reasonDetail);
        }

        /// <summary>Placeholder for future row-level vet/active audit API. Returns false until implemented.</summary>
        public static bool TryResolveFromAuditLog(TransInterviewer iv, string reasonDetail, out DmIvInactiveReasonResult result)
        {
            result = null;
            return false;
        }

        internal static DmIvInactiveReasonResult ResolveFromCurrentState(TransInterviewer iv, bool channelLocked, string reasonDetail)
        {
            var rejected = string.Equals(iv.ClientAction, "Rejected", StringComparison.OrdinalIgnoreCase);
            var rbVetYes = iv.IsSMEVerified || iv.IsSMEVerifiedStored == true;
            var rbVetExplicitNo = iv.IsSMEVerifiedStored == false;
            var everActivated = WasEverActivatedOnChannel(iv);

            // (a) Lock queue: never activated on channel, available, awaiting Risebird vet.
            if (!everActivated && channelLocked && iv.IsAvailable && !rejected && !rbVetYes && !rbVetExplicitNo)
                return DmIvInactiveReasonResult.ForCategory(DmIvInactiveReasonCategory.PendingRbReview, reasonDetail);

            if (!everActivated && channelLocked && iv.IsAvailable && !rejected && !rbVetYes)
                return DmIvInactiveReasonResult.ForCategory(DmIvInactiveReasonCategory.PendingRbReview, reasonDetail);

            // (c) Client removed while RB vet remained Yes.
            if (everActivated && rejected && rbVetYes)
                return DmIvInactiveReasonResult.ForCategory(DmIvInactiveReasonCategory.RemovedByClient, reasonDetail);

            // (b) RB removed (explicit No or reject cascade without RB Yes).
            if (everActivated && (rbVetExplicitNo || (rejected && !rbVetYes)))
                return DmIvInactiveReasonResult.ForCategory(DmIvInactiveReasonCategory.RemovedByRb, reasonDetail);

            // (d) Legacy / manual deactivation / missing ActivatedAt — do not guess.
            return DmIvInactiveReasonResult.ForCategory(DmIvInactiveReasonCategory.Unknown, reasonDetail);
        }
    }
}
