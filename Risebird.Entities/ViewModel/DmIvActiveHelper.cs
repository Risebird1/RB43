using System;

namespace Risebird.Entities.ViewModel
{
    public static class DmIvActiveHelper
    {
        public static bool IsClientVetYes(string clientAction) =>
            string.Equals(clientAction, "Shortlisted", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// active = (rbVet === yes) AND (clientVet !== no)
        /// </summary>
        public static bool ComputeActive(bool rbVetYes, bool clientVetYes) =>
            rbVetYes && clientVetYes;

        public static bool ComputeActive(bool rbVetYes, string clientAction) =>
            ComputeActive(rbVetYes, IsClientVetYes(clientAction));

        public static bool IsAwaitingRbVet(bool rbVetYes, string clientAction) =>
            !rbVetYes && !string.Equals(clientAction, "Rejected", StringComparison.OrdinalIgnoreCase);
    }
}
