using Risebird.Entities.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IWhatsappRepository
    {
        Task<bool> SaveSentWhatsappMessageDetails(SentWhatsAppMessageDetails model);

        // WhatsApp-only opt-out (dbo.WhatsAppOptOut). Candidates are scoped per IRF; a row with IRFId NULL covers all IRFs.
        Task<bool> IsOptedOutAsync(string audience, string email, int? irfId, int? prospectId = null);
        // templateEvent: the message the unsubscribe came from (the link's src). An active opt-out is kept as it is.
        Task<bool> SaveOptOutAsync(string audience, string email, int? irfId, int? prospectId, string source, string templateEvent = null);
        // The optional reason from the confirmation page, on the person's active opt-out for that scope.
        Task<bool> SetOptOutReasonAsync(string audience, string email, int? irfId, string reason);

        // Cold candidate (dbo.ColdCandidateInfos) for a WhatsApp opt-out: mark, or undo on re-enable.
        // irfId NULL = every IRF of the candidate (a Stop reply).
        // coldSource: ColdCandidateInfos.ColdSource, e.g. "WhatsApp unsubscribe button (Schedule Interview)".
        Task MarkCandidateColdAsync(string email, int? irfId, string markedBy, string coldInfo, string coldSource);
        Task UndoCandidateColdAsync(string email, int? irfId, string undoneBy);
        // The reason from the unsubscribe page into ColdCandidateInfos.ColdReason (and ColdInfo).
        Task<bool> UpdateColdReasonAsync(string email, int irfId, string reason);

        // Interviewer Churn Data for a WhatsApp opt-out (InterviewerProfile.ChurnSource / ChurnReason / ChurnDate);
        // availability and the account are not changed. Undo clears only a WhatsApp churn mark.
        Task MarkInterviewerChurnAsync(string email, string churnSource);
        Task<bool> SetInterviewerChurnReasonAsync(string email, string reason);
        Task UndoInterviewerChurnAsync(string email);

        // Stop replies: the portal users whose WhatsApp number ends with these digits.
        Task<List<WhatsAppNumberOwner>> FindUsersByWhatsAppNumberAsync(string lastDigits);

        // Successful sends of one event to one candidate for one IRF (the 3-per-stage limit).
        Task<int> CountSentAsync(string email, int irfId, string eventName);

        // Opt-out and limit usage of many (email, IRF) pairs in one query, for the WhatsApp send buttons on the lists.
        Task<List<WhatsAppSendStatusRow>> GetSendStatusAsync(IEnumerable<(string Email, int IRFId)> keys, string eventName);

        // Admin re-enable: search opt-outs (active and history) and stamp one as re-enabled.
        Task<List<WhatsAppOptOutRow>> SearchOptOutsAsync(string search, bool includeHistory);
        Task<WhatsAppOptOutRow> GetOptOutAsync(int id);
        Task<bool> ReEnableOptOutAsync(int id, string reEnabledBy);

        Task<string> FetchUniqueIdByEmailAsync(string email);
        Task<string> FetchEmailByUniqueIdAsync(string uniqueId);
        Task<int> FetchIrfIdByInterviewIdAsync(int interviewId);
    }
}
