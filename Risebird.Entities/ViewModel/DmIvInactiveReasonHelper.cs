using System;

namespace Risebird.Entities.ViewModel
{
    public enum DmIvInactiveReasonCategory
    {
        PendingRbReview,
        AwaitingClientApproval,
        RemovedByRb,
        RemovedByClient,
        Unavailable,
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
                case DmIvInactiveReasonCategory.AwaitingClientApproval:
                    return new DmIvInactiveReasonResult(category, "Awaiting client approval", "info");
                case DmIvInactiveReasonCategory.RemovedByRb:
                    return new DmIvInactiveReasonResult(category, "Removed by RB" + suffix, "warning");
                case DmIvInactiveReasonCategory.RemovedByClient:
                    return new DmIvInactiveReasonResult(category, "Removed by Client" + suffix, "warning");
                case DmIvInactiveReasonCategory.Unavailable:
                    return new DmIvInactiveReasonResult(category, "Not available on channel", "muted");
                default:
                    return new DmIvInactiveReasonResult(DmIvInactiveReasonCategory.Unknown, "Inactive — reason unknown", "unknown");
            }
        }
    }

    /// <summary>
    /// Derives inactive-tab badge labels from interviewer-channel state.
    /// </summary>
    public static class DmIvInactiveReasonHelper
    {
        /// <summary>Reached active panel: activation timestamp or conducted interviews / outcomes on this channel.</summary>
        public static bool WasEverOnActivePanel(TransInterviewer iv)
        {
            if (iv == null) return false;

            if (iv.ActivatedAt != null && iv.ActivatedAt.Value.Year >= 1900)
                return true;

            if (iv.TotalCompletedMeetings > 0 || iv.TotalInterview > 0)
                return true;

            if (iv.TotalSelected > 0 || iv.TotalRejected > 0)
                return true;

            return false;
        }

        /// <summary>Used for row data-ever-activated — panel history OR both vets satisfied (then later deactivated).</summary>
        public static bool WasEverActivatedOnChannel(TransInterviewer iv)
        {
            if (iv == null) return false;
            if (WasEverOnActivePanel(iv)) return true;
            if (HasRbApproval(iv) && HasClientApproval(iv)) return true;
            return false;
        }

        public static bool HasRbApproval(TransInterviewer iv) =>
            iv != null && (iv.IsSMEVerified || iv.IsSMEVerifiedStored == true);

        public static bool HasClientApproval(TransInterviewer iv) =>
            iv != null && DmIvActiveHelper.IsClientVetYes(iv.ClientAction);

        /// <summary>Historical RB vet from DB (not display-only vet for active rows). Token: 1 | 0 | unset.</summary>
        public static string GetHistoricalRbVetDbToken(TransInterviewer iv)
        {
            if (iv == null) return "unset";
            if (iv.IsSMEVerifiedStored == false) return "0";
            if (iv.IsSMEVerifiedStored == true || iv.IsSMEVerified) return "1";
            return "unset";
        }

        public static bool ResolveHistoricalRbVetYes(TransInterviewer iv) => HasRbApproval(iv);

        public static bool ResolveHistoricalRbVetExplicitNo(TransInterviewer iv) =>
            iv?.IsSMEVerifiedStored == false;

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
            var rbVetYes = HasRbApproval(iv);
            var rbVetExplicitNo = ResolveHistoricalRbVetExplicitNo(iv);
            var rbVetUnset = iv.IsSMEVerifiedStored == null && !iv.IsSMEVerified;
            var clientApproved = HasClientApproval(iv);
            var onPanel = WasEverOnActivePanel(iv);

            // (a) Lock queue: not RB-vetted yet, still available to review.
            if (!rbVetYes && !rbVetExplicitNo && channelLocked && iv.IsAvailable && !rejected && !onPanel)
                return DmIvInactiveReasonResult.ForCategory(DmIvInactiveReasonCategory.PendingRbReview, reasonDetail);

            // RB approved, client not yet — still in approval flow (not a removal).
            if (rbVetYes && !clientApproved && !rejected && !onPanel)
                return DmIvInactiveReasonResult.ForCategory(DmIvInactiveReasonCategory.AwaitingClientApproval, reasonDetail);

            // (c) Client removed while RB vet remained Yes.
            if (onPanel && rejected && rbVetYes)
                return DmIvInactiveReasonResult.ForCategory(DmIvInactiveReasonCategory.RemovedByClient, reasonDetail);

            if (rejected && rbVetYes && !onPanel && clientApproved)
                return DmIvInactiveReasonResult.ForCategory(DmIvInactiveReasonCategory.RemovedByClient, reasonDetail);

            // (b) RB vet No or rejected without RB Yes.
            if (rbVetExplicitNo || (rejected && !rbVetYes))
                return DmIvInactiveReasonResult.ForCategory(DmIvInactiveReasonCategory.RemovedByRb, reasonDetail);

            if (onPanel && rejected && rbVetUnset)
                return DmIvInactiveReasonResult.ForCategory(DmIvInactiveReasonCategory.RemovedByRb, reasonDetail);

            // Was on panel (or fully approved then deactivated) — ops off-panel; DB may still show approved flags.
            if (onPanel && !rejected)
                return DmIvInactiveReasonResult.ForCategory(DmIvInactiveReasonCategory.RemovedByRb, reasonDetail);

            if (rbVetYes && clientApproved && !rejected)
                return DmIvInactiveReasonResult.ForCategory(DmIvInactiveReasonCategory.RemovedByRb, reasonDetail);

            // Never vetted, not available — declined availability or never joined queue (e.g. SuGu1639).
            if (!rbVetYes && !clientApproved && !onPanel && !iv.IsAvailable && !rejected)
                return DmIvInactiveReasonResult.ForCategory(DmIvInactiveReasonCategory.Unavailable, reasonDetail);

            return DmIvInactiveReasonResult.ForCategory(DmIvInactiveReasonCategory.Unknown, reasonDetail);
        }
    }
}
