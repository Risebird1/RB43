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
        /// </summary>
        bool VerifyWebhookSignature(string rawBody, string signatureHeader);
    }
}
