using Risebird.Entities.Data.PlivoWhatsAppModels;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IPlivoWhatsAppService
    {
        /// <summary>
        /// Sends a WhatsApp template message for interview scheduling.
        /// </summary>
        Task<PlivoWhatsAppSendResponse> SendScheduleMessageAsync(PlivoWhatsAppTriggerContext context);

        /// <summary>
        /// Sends a WhatsApp template message for interview rescheduling.
        /// </summary>
        Task<PlivoWhatsAppSendResponse> SendRescheduleMessageAsync(PlivoWhatsAppTriggerContext context);

        /// <summary>
        /// Sends a free-form session message (within 24-hour reply window).
        /// Used for clarification or confirmation follow-ups.
        /// </summary>
        Task<PlivoWhatsAppSendResponse> SendSessionMessageAsync(string toPhone, string text);

        /// <summary>
        /// Triggers an arbitrary Plivo CX WhatsApp flow by its full trigger URL,
        /// POSTing the supplied payload object as JSON. Used by the AI Phone Screener
        /// where each skill channel maps to its own WhatsApp agent trigger URL
        /// (IRF.PlivoWhatsAppAssistantUrl). Mirrors PlivoCXService.TriggerByUrlAsync.
        /// </summary>
        Task<PlivoWhatsAppSendResponse> TriggerByUrlAsync(string triggerUrl, object payload);
    }
}
