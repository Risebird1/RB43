using System.Collections.Generic;
using System.Threading.Tasks;
using Risebird.Entities.Data.ElevenLabsModels;

namespace Risebird.CommonHelper.Interface
{
    /// <summary>
    /// Talks to the ElevenLabs Agents Platform for the WhatsApp phone screener.
    /// Starts outbound WhatsApp conversations (template-initiated) driven by an
    /// ElevenLabs agent, and verifies inbound post-call webhook signatures.
    /// </summary>
    public interface IElevenLabsAgentService
    {
        /// <summary>
        /// Sends the approved WhatsApp template to open the conversation and hands the
        /// candidate to the given ElevenLabs agent. Dynamic variables (e.g. log_id,
        /// candidate context) are echoed back on the post-call webhook.
        /// </summary>
        /// <param name="agentId">The ElevenLabs agent id mapped to the skill channel.</param>
        /// <param name="candidatePhone">Candidate phone; sanitized to digits-only international form.</param>
        /// <param name="dynamicVariables">Values echoed back on the webhook (must include log_id).</param>
        /// <param name="templateBodyParams">Ordered body parameters for the approved template.</param>
        Task<ElevenLabsWhatsAppOutboundResponse> SendWhatsAppScreenAsync(
            string agentId,
            string candidatePhone,
            IDictionary<string, object> dynamicVariables,
            IList<string> templateBodyParams);

        /// <summary>
        /// Verifies the ElevenLabs-Signature header (HMAC-SHA256) against the raw body.
        /// Returns true when no secret is configured (so the feature works before the
        /// secret is set), and true only on a valid signature once a secret exists.
        /// Accepts the main account's secret and the campaign account's (ElevenLabs:Campaigns:WebhookSecret).
        /// </summary>
        bool VerifyWebhookSignature(string rawBody, string signatureHeader);

        // ── Voice campaigns (ElevenLabs:Campaigns) ───────────────────────────────

        /// <summary>
        /// Places an outbound voice call with the given agent through the campaign telephony line
        /// (ElevenLabs:Campaigns:Telephony - PlivoSip, Twilio or Exotel) from its caller number
        /// (ElevenLabs:Campaigns:PhoneNumberId). Dynamic variables fill the agent prompt's
        /// {{placeholders}} and come back on the post-call webhook. Never throws.
        /// </summary>
        Task<ElevenLabsOutboundCallResponse> PlaceCampaignCallAsync(
            string agentId, string toNumber, IDictionary<string, object> dynamicVariables);

        /// <summary>Creates an agent in the campaign account. Never throws.</summary>
        Task<ElevenLabsCreateAgentResponse> CreateCampaignAgentAsync(ElevenLabsCreateAgentRequest request);

        /// <summary>
        /// The id of the campaign-account agent with exactly this name, or null when there is none
        /// (or the lookup fails). Lets agent setup update an agent it created earlier instead of
        /// creating a duplicate. Never throws.
        /// </summary>
        Task<string> FindCampaignAgentIdByNameAsync(string name);

        /// <summary>Updates an agent (PATCH, full payload) in the campaign account. Never throws.</summary>
        Task<ElevenLabsCreateAgentResponse> UpdateCampaignAgentAsync(string agentId, ElevenLabsCreateAgentRequest request);

        /// <summary>Why campaign calls cannot be placed (missing key / caller number / bad line), or null when ready.</summary>
        string CampaignCallingProblem { get; }
    }
}
