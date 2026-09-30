using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Entities.Data
{
    /// <summary>
    /// "Exotel" section of appsettings — Exotel as a calling provider ("Call via: Exotel" in the call modal).
    /// ApiKey / ApiToken are secrets: keep them in user-secrets / environment variables
    /// (Exotel__ApiKey, Exotel__ApiToken), never in the committed appsettings.json.
    /// </summary>
    public class ExotelSettings
    {
        /// <summary>Region cluster: "api.exotel.com" (Singapore) or "api.in.exotel.com" (Mumbai).</summary>
        public string Subdomain { get; set; } = "api.exotel.com";
        public string AccountSid { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiToken { get; set; } = string.Empty;
        /// <summary>ExoPhone (Exotel virtual number) the candidate sees as caller ID.</summary>
        public string CallerId { get; set; } = string.Empty;
        public bool Record { get; set; } = true;
        /// <summary>Make Exotel the default provider in the call modal (otherwise Plivo, then Twilio).</summary>
        public bool IsDefaultProvider { get; set; }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Subdomain) && !string.IsNullOrWhiteSpace(AccountSid)
            && !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ApiToken)
            && !string.IsNullOrWhiteSpace(CallerId);
    }

    /// <summary>
    /// "ExotelWebRtc" section — Path A (in-browser softphone via the Exotel Voice WebSDK).
    /// Needs Exotel's "IP-PSTN intermix" onboarding and one SIP subscriber per agent
    /// (Subscriber Management API). Leave empty to use click-to-call (agent's phone) only.
    /// </summary>
    public class ExotelWebRtcSettings
    {
        /// <summary>SIP domain from onboarding, e.g. "&lt;accountSid&gt;.voip.exotel.com".</summary>
        public string SipDomain { get; set; } = string.Empty;
        /// <summary>WebSocket host:port the SDK connects to, from onboarding (e.g. "voip.in1.exotel.com:443").</summary>
        public string WssHost { get; set; } = string.Empty;
        /// <summary>Where the exotelsdk.js bundle (with its .wav files next to it) is served from.</summary>
        public string SdkUrl { get; set; } = "/lib/exotel/exotelsdk.js";
        /// <summary>
        /// Agent → SIP subscriber map. Exotel's Subscriber Management API never returns passwords, so each
        /// subscriber's password is kept here. Secrets — env / user-secrets (ExotelWebRtc__Subscribers__0__Password).
        /// Agents without an entry fall back to click-to-call.
        /// </summary>
        public List<ExotelSubscriber> Subscribers { get; set; } = new List<ExotelSubscriber>();

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(SipDomain) && !string.IsNullOrWhiteSpace(WssHost)
            && !string.IsNullOrWhiteSpace(SdkUrl) && Subscribers != null && Subscribers.Count > 0;

        public ExotelSubscriber FindSubscriber(string agentEmail)
        {
            if (string.IsNullOrWhiteSpace(agentEmail) || Subscribers == null)
                return null;
            return Subscribers.FirstOrDefault(s => s != null
                && string.Equals(s.Email?.Trim(), agentEmail.Trim(), StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(s.UserName) && !string.IsNullOrWhiteSpace(s.Password));
        }
    }

    public class ExotelSubscriber
    {
        /// <summary>Portal login email of the agent.</summary>
        public string Email { get; set; } = string.Empty;
        /// <summary>SIP user name of the agent's Exotel subscriber.</summary>
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
