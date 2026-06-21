using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Read-path display rules for RB Vet / Client Vet on DM Approval.
    /// Does not write to the database — only resolves what the UI should show.
    /// </summary>
    public static class DmIvVetDisplayHelper
    {
        /// <summary>
        /// RB Vet Yes for display. Only active panel rows can show Yes.
        /// Inactive / available-only rows stay No even if IsSMEVerified is still true in DB from an earlier state.
        /// On auto-activate channels, active+available rows with NULL vet in DB are treated as Yes (legacy implicit panel).
        /// </summary>
        public static bool ResolveRbVetYes(bool channelLocked, TransInterviewer iv)
        {
            if (iv == null) return false;
            if (iv.IsSMEVerifiedStored == false) return false;

            if (!iv.IsActive)
                return false;

            if (iv.IsSMEVerifiedStored == true || iv.IsSMEVerified) return true;

            // Preserved panel: active on channel before lock flip — implicit vet must still display Yes.
            if (iv.IsActive && iv.IsAvailable
                && !string.Equals(iv.ClientAction, "Rejected", StringComparison.OrdinalIgnoreCase)
                && iv.IsSMEVerifiedStored == null)
                return true;

            if (!channelLocked && iv.IsAvailable
                && !string.Equals(iv.ClientAction, "Rejected", StringComparison.OrdinalIgnoreCase)
                && iv.IsSMEVerifiedStored == null)
                return true;

            return false;
        }

        /// <summary>
        /// Client Vet Yes for display. Only active panel rows can show Yes.
        /// </summary>
        public static bool ResolveClientVetYes(bool channelLocked, TransInterviewer iv, bool rbVetYes)
        {
            if (iv == null) return false;
            if (string.Equals(iv.ClientAction, "Rejected", StringComparison.OrdinalIgnoreCase)) return false;

            if (!iv.IsActive)
                return false;

            if (DmIvActiveHelper.IsClientVetYes(iv.ClientAction)) return true;

            // Preserved active panel on locked channel (implicit client approval before lock).
            if (channelLocked && iv.IsActive && rbVetYes && iv.IsAvailable) return true;

            if (!channelLocked && rbVetYes && iv.IsAvailable) return true;

            return false;
        }

        /// <summary>
        /// Stored RB vet for row data attributes and vet-toggle baseline (not display-only derivation).
        /// Inactive rows always store No — historical IsSMEVerified in DB does not apply off the active panel.
        /// </summary>
        public static bool ResolveRbVetStored(TransInterviewer iv)
        {
            if (iv == null || !iv.IsActive) return false;
            if (iv.IsSMEVerifiedStored == false) return false;
            if (iv.IsSMEVerifiedStored == true || iv.IsSMEVerified) return true;
            if (iv.IsActive && iv.IsAvailable
                && !string.Equals(iv.ClientAction, "Rejected", StringComparison.OrdinalIgnoreCase)
                && iv.IsSMEVerifiedStored == null)
                return true;
            return false;
        }

        /// <summary>
        /// Stored Client vet for row data attributes and vet-toggle baseline.
        /// Inactive rows always store No.
        /// </summary>
        public static bool ResolveClientVetStored(TransInterviewer iv, bool rbVetStored)
        {
            if (iv == null || !iv.IsActive) return false;
            if (string.Equals(iv.ClientAction, "Rejected", StringComparison.OrdinalIgnoreCase)) return false;
            if (DmIvActiveHelper.IsClientVetYes(iv.ClientAction)) return true;
            if (iv.IsActive && rbVetStored && iv.IsAvailable) return true;
            return false;
        }
    }
}
