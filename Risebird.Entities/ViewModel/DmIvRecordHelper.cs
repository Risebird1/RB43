using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Central rules for interviewer-channel active/available/vet state.
    /// Used on vet toggles (server) and documents defaults for new rows (insert SPs).
    /// Does not run on existing rows at deploy — only user-triggered flips and new inserts.
    /// </summary>
    public static class DmIvRecordHelper
    {
        /// <summary>
        /// Defaults when a new interviewer-channel row is created (apply / add paths).
        /// Free channel: available + active immediately. Locked: available, not active, vets undecided.
        /// </summary>
        public static (bool isActive, bool isAvailable, bool isSmeVerified, string clientAction) GetNewRecordState(bool channelLocked)
        {
            if (channelLocked)
                return (false, true, false, null);
            return (true, true, true, "Shortlisted");
        }

        /// <summary>
        /// After a vet toggle: both flags derive from vet state. Satisfied cascade → both true; otherwise both false.
        /// </summary>
        public static (bool isActive, bool isAvailable) ComputeFlagsFromVet(bool rbVetYes, string clientAction)
        {
            if (DmIvActiveHelper.ComputeActive(rbVetYes, clientAction))
                return (true, true);
            return (false, false);
        }
    }
}
