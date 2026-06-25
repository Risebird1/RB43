using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Role routing for Client Report "Remove Interviewer" — server session profileName only.
    /// </summary>
    public static class DmIvClientReportRoleHelper
    {
        public static bool IsClientRole(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                return false;
            return string.Equals(profileName.Trim(), "Client", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>DM, SME, ops, and other authenticated internal Risebird roles (not Client/Public).</summary>
        public static bool IsInternalRole(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                return false;
            var p = profileName.Trim();
            if (IsClientRole(p))
                return false;
            if (string.Equals(p, "Public", StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }
    }
}
