using Risebird.Entities.Data.VapiModels;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    /// <summary>
    /// Service interface for Vapi.ai voice agent operations
    /// </summary>
    public interface IVapiService
    {
        /// <summary>
        /// Creates an assistant for interview booking
        /// </summary>
        Task<string> CreateInterviewBookingAssistantAsync(string assistantName, string webhookUrl);

        /// <summary>
        /// Initiates an outbound call to a candidate
        /// </summary>
        Task<VapiCallResponse> InitiateCallAsync(VapiCallRequest request);

        /// <summary>
        /// Gets call details by call ID
        /// </summary>
        Task<VapiCallResponse> GetCallDetailsAsync(string callId);

        /// <summary>
        /// Lists all assistants
        /// </summary>
        Task<string> ListAssistantsAsync();

        /// <summary>
        /// Deletes a call recording
        /// </summary>
        Task<bool> DeleteCallAsync(string callId);

        /// <summary>
        /// Processes tool call webhook from Vapi
        /// </summary>
        Task<VapiToolCallResult> ProcessToolCallAsync(VapiWebhookMessage webhookMessage);
    }
}
