using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ITransInterviewerAuditRepository
    {
        Task InsertAsync(int transInterviewerId, string eventType, string fromValue, string toValue,
            string reasonCategory, int? triggeringInterviewId, string changedBy,
            string auditTag = null, string auditSource = null, Guid? backfillBatchId = null, string extraMetadata = null);

        Task UpdateAutoQcFieldsAsync(int transInterviewerId, string autoQcVerdict, int triggeringInterviewId);

        Task ClearAutoQcFieldsAsync(int transInterviewerId);

        Task<TransInterviewerInterviewContext> GetContextForInterviewAsync(int interviewId);

        Task<IReadOnlyList<TransInterviewerInactiveAuditSummary>> GetInactiveAuditSummariesByIrfIdAsync(int irfId);

        Task<bool> HasAutoQcRemovalAuditForInterviewAsync(int transInterviewerId, int triggeringInterviewId, string auditSource = null);
    }
}
