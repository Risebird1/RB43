using System;
using System.Linq;

namespace Risebird.Web.Helpers
{
    /// <summary>
    /// Who may use the AI Audio Adaptive HR screening intake (/AIScreening/New).
    ///
    /// Shared by the controller, which enforces it, and the views that offer the entry point, so a
    /// button is never shown to someone who would only be bounced to AccessDenied by clicking it.
    /// </summary>
    public static class AIScreeningAccess
    {
        /// <summary>
        /// The same set the existing create-from-template action allows. Clients are deliberately
        /// excluded: this path provisions a channel and picks its billing owner.
        /// </summary>
        public static readonly string[] AllowedProfiles =
        {
            "Admin", "SupportAdmin", "HR", "SeniorHR",
            "InterviewerManagement", "SeniorInterviewerManagement"
        };

        public static bool IsAllowed(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName)) return false;

            var profile = profileName.Trim().Replace("\"", string.Empty);
            return AllowedProfiles.Contains(profile, StringComparer.OrdinalIgnoreCase);
        }
    }
}
