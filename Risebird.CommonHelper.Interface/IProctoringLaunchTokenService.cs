using System;

namespace Risebird.CommonHelper.Interface
{
    /// <summary>
    /// Signs DesktopProctor launch tokens: v1.&lt;base64url(payload JSON)&gt;.&lt;base64url(ES256 signature)&gt;,
    /// ES256 over an ECDSA P-256 key using DSASignatureFormat.IeeeP1363FixedFieldConcatenation — the exact
    /// scheme the desktop client's LaunchTokenValidator verifies.
    /// </summary>
    public interface IProctoringLaunchTokenService
    {
        /// <summary>Mints and signs a launch token for a fresh session. Returns the token string and its expiry.</summary>
        (string LaunchToken, DateTime ExpiresAtUtc) MintLaunchToken(
            string apiBaseUrl,
            string sessionToken,
            string interviewId,
            string candidateEmail,
            string browserSessionId,
            TimeSpan ttl);
    }
}
