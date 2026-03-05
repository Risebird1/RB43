using Risebird.Entities.Data.PlivoModels;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    /// <summary>
    /// Service interface for Plivo voice API operations
    /// </summary>
    public interface IPlivoService
    {
        /// <summary>
        /// Initiates an outbound call via Plivo REST API.
        /// Returns the Plivo request_uuid (NOT the CallUUID — that comes via answer_url callback).
        /// </summary>
        Task<PlivoOutboundCallResponse> InitiateCallAsync(PlivoOutboundCallRequest request, int? candidateId = null);

        /// <summary>
        /// Fetches completed call details from Plivo CDR by CallUUID
        /// </summary>
        Task<PlivoCallDetails> GetCallDetailsAsync(string callUUID);

        /// <summary>
        /// Terminates an active call by CallUUID
        /// </summary>
        Task<bool> HangupCallAsync(string callUUID);

        /// <summary>
        /// Generates PlivoXML with a bidirectional audio stream pointed at our WebSocket endpoint
        /// </summary>
        string BuildAnswerXml(string webSocketUrl, string statusCallbackUrl);
    }
}
