using Risebird.Entities.Data;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IWhatsappRepository
    {
        Task<bool> SaveSentWhatsappMessageDetails(SentWhatsAppMessageDetails model);

        // WhatsApp-only opt-out (dbo.WhatsAppOptOut). Candidates are scoped per IRF; a row with IRFId NULL covers all IRFs.
        Task<bool> IsOptedOutAsync(string audience, string email, int? irfId, int? prospectId = null);
        Task<bool> SaveOptOutAsync(string audience, string email, int? irfId, int? prospectId, string source);

        // Successful sends of one event to one candidate for one IRF (the 3-per-stage limit).
        Task<int> CountSentAsync(string email, int irfId, string eventName);

        Task<string> FetchUniqueIdByEmailAsync(string email);
        Task<string> FetchEmailByUniqueIdAsync(string uniqueId);
        Task<int> FetchIrfIdByInterviewIdAsync(int interviewId);
    }
}
