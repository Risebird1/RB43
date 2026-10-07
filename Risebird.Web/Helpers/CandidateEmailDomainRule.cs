using System;

namespace Risebird.Web.Helpers
{
    /// <summary>
    /// A candidate's email may not be on @risebird.io nor on the domain of the skill channel's billing owner
    /// (the client's own staff are not candidates).
    ///
    /// Shared by CandidateController (Add / Edit) and the Fix Email action of the Bounce &amp; Dropped Mail page.
    /// </summary>
    public static class CandidateEmailDomainRule
    {
        public const string RisebirdDomain = "risebird.io";

        /// <summary>Domain of the billing owner's email (CandidateService.FetchBillingownerById), or risebird.io when none.</summary>
        public static string GetClientDomain(string billingOwnerEmail)
        {
            if (string.IsNullOrWhiteSpace(billingOwnerEmail) || !billingOwnerEmail.Contains("@"))
            {
                return RisebirdDomain;
            }
            return billingOwnerEmail.Split("@")[1].Trim();
        }

        public static bool IsBlocked(string candidateEmail, string clientDomain)
        {
            if (string.IsNullOrWhiteSpace(candidateEmail))
            {
                return false;
            }
            var parts = candidateEmail.Trim().Split("@");
            foreach (var part in parts)
            {
                if (string.Equals(part, RisebirdDomain, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrWhiteSpace(clientDomain) && string.Equals(part, clientDomain, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
            return false;
        }

        public static string BlockedMessage(string clientDomain)
        {
            return clientDomain == RisebirdDomain
                ? "Domain @Risebird.io is prohibited for a candidate"
                : "Domain @Risebird.io and " + clientDomain + " is prohibited for a candidate";
        }
    }
}
