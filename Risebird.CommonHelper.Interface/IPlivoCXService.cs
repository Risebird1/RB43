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

        /// <summary>
        /// Triggers the onboarding recruitment call to a prospective interviewer.
        /// Uses PlivoCX:OnboardingTriggerEndpointUrl.
        /// </summary>
        Task<PlivoCXTriggerResponse> TriggerOnboardingCallAsync(PlivoCXOnboardingTriggerRequest request);

        /// <summary>
        /// Triggers the availability check call to an existing interviewer.
        /// Uses PlivoCX:AvailabilityTriggerEndpointUrl.
        /// </summary>
        Task<PlivoCXTriggerResponse> TriggerAvailabilityCallAsync(PlivoCXAvailabilityTriggerRequest request);

        /// <summary>
        /// Triggers an outbound call against an explicitly supplied Plivo CX flow trigger URL.
        /// Used by the Phone Screener, where each skill channel (IRF) maps to its own
        /// Plivo assistant URL rather than a single global endpoint in appsettings.
        /// </summary>
        Task<PlivoCXTriggerResponse> TriggerByUrlAsync(string triggerUrl, object request);

        /// <summary>
        /// Fetches the call recording URL and (best-effort) transcript for a completed
        /// call using Plivo's Voice REST APIs (Recording API + Transcription API),
        /// keyed by the Plivo call UUID. Fallback for when the Recording URL event
        /// callback did not deliver these to our webhook. Never throws.
        ///
        /// If a transcription does not exist yet, one is requested asynchronously with
        /// <paramref name="transcriptionCallbackUrl"/> so Plivo posts the finished
        /// transcript back to our webhook (it includes the call UUID for resolution).
        /// </summary>
        Task<PhoneScreenMediaResult> FetchCallMediaAsync(string callUuid, string transcriptionCallbackUrl = null);

        /// <summary>
        /// Recording-only lookup: returns the Plivo recording URL for a call UUID via the
        /// Recording API, or null when there is no recording yet (it finalises shortly after
        /// hangup). Unlike <see cref="FetchCallMediaAsync"/> it never calls the Transcription
        /// API, so it incurs no transcription cost. Never throws.
        /// </summary>
        Task<string> GetRecordingUrlAsync(string callUuid);
    }
}
