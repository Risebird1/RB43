using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    /// <summary>
    /// Business-layer interface for Plivo AI interview calling operations
    /// </summary>
    public interface IPlivoInterviewService
    {
        Task<List<PlivoCallLog>> InitiateBatchCallsAsync(List<RecipientCallInfo> recipients);
        Task<bool> ProcessInterviewBookingAsync(string callUUID, DateTime interviewDateTime);
        Task<List<PlivoCallLog>> GetPendingCallbacksAsync();
    }
}
