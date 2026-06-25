using Risebird.Entities.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ITransInterviewerAuditRepository
    {
        Task InsertAsync(int transInterviewerId, string eventType, string fromValue, string toValue,
            string reasonCategory, int? triggeringInterviewId, string changedBy);

        Task UpdateAutoQcFieldsAsync(int transInterviewerId, string autoQcVerdict, int triggeringInterviewId);

        Task ClearAutoQcFieldsAsync(int transInterviewerId);

        Task<TransInterviewerInterviewContext> GetContextForInterviewAsync(int interviewId);

        Task<IReadOnlyList<TransInterviewerInactiveAuditSummary>> GetInactiveAuditSummariesByIrfIdAsync(int irfId);
    }
}
