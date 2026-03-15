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
    }
}
