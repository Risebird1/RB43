using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    /// <summary>
    /// Data access for the AI Phone Screener feature. Kept isolated from the large
    /// IRF save/load stored procedures: the Plivo assistant URL is read/written via
    /// a dedicated single-column update so the existing IRF flows are untouched.
    /// </summary>
    public interface IPhoneScreenRepository
    {
        // ── Skill channel (IRF) -> Plivo assistant URL mapping ──────────────
        Task<string> GetIRFPlivoAssistantUrlAsync(int irfId);
        Task<bool> SetIRFPlivoAssistantUrlAsync(int? irfId, string irfCode, string url);

        // ── Skill channel (IRF) -> Plivo WhatsApp assistant URL mapping ─────
        Task<string> GetIRFPlivoWhatsAppAssistantUrlAsync(int irfId);
        Task<bool> SetIRFPlivoWhatsAppAssistantUrlAsync(int? irfId, string irfCode, string url);

        Task<PhoneScreenIRFHeader> GetIRFHeaderAsync(int irfId);

        // ── Phone screen call records ───────────────────────────────────────
        Task<int> InsertCallAsync(PhoneScreenCall call);
        Task<PhoneScreenCall> GetCallByIdAsync(int id);
        Task<PhoneScreenCall> GetCallByConversationIdAsync(string conversationId);
        Task<PhoneScreenCall> GetCallByExternalCallIdAsync(string externalCallId);
        Task<IEnumerable<PhoneScreenCall>> GetCallsByIRFAsync(int irfId);
        Task<bool> UpdateCallTriggerResultAsync(int id, string status, string conversationId, string externalCallId);
        Task<bool> UpdateCallFromWebhookAsync(PhoneScreenCall data);
    }
}
