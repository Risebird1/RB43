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
        /// RB Vet Yes for display. Uses stored IsSMEVerified when set; on free channels,
        /// active+available rows with NULL vet in DB are treated as Yes (legacy / implicit panel).
        /// </summary>
        public static bool ResolveRbVetYes(bool channelLocked, TransInterviewer iv)
        {
            if (iv == null) return false;
            if (iv.IsSMEVerifiedStored == true || iv.IsSMEVerified) return true;
            if (iv.IsSMEVerifiedStored == false) return false;

            if (!channelLocked && iv.IsActive && iv.IsAvailable
                && !string.Equals(iv.ClientAction, "Rejected", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        /// <summary>
        /// Client Vet Yes for display. Uses stored ClientAction when set; on free channels,
        /// active+available rows with RB Vet Yes and no explicit rejection show Yes.
        /// </summary>
        public static bool ResolveClientVetYes(bool channelLocked, TransInterviewer iv, bool rbVetYes)
        {
            if (iv == null) return false;
            if (DmIvActiveHelper.IsClientVetYes(iv.ClientAction)) return true;
            if (string.Equals(iv.ClientAction, "Rejected", StringComparison.OrdinalIgnoreCase)) return false;

            if (!channelLocked && rbVetYes && iv.IsActive && iv.IsAvailable) return true;

            return false;
        }
    }
}
