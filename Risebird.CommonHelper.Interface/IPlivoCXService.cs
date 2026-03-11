using Risebird.Entities.Data.PlivoCXModels;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    /// <summary>
    /// Calls the Plivo CX "API Request" trigger endpoint to start an outbound AI voice call.
    /// Plivo CX's own AI agent (configured in the CX dashboard) then manages the conversation.
    /// </summary>
    public interface IPlivoCXService
    {
        /// <summary>
        /// POSTs to the configured Plivo CX flow trigger URL, passing candidate context as
        /// variables that the AI Conversation node will inject into its instructions.
        /// Returns the trigger response including a conversation_id.
        /// </summary>
        Task<PlivoCXTriggerResponse> TriggerOutboundCallAsync(PlivoCXTriggerRequest request);

        /// <summary>
        /// Same as TriggerOutboundCallAsync but targets the separate reschedule agent
        /// (configured via PlivoCX:RescheduleTriggerEndpointUrl in appsettings.json).
        /// </summary>
        Task<PlivoCXTriggerResponse> TriggerRescheduleCallAsync(PlivoCXTriggerRequest request);
    }
}
