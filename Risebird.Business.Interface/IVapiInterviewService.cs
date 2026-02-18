using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    /// <summary>
    /// Business service interface for Vapi interview calling operations
    /// </summary>
    public interface IVapiInterviewService
    {
        /// <summary>
        /// Initiates interview booking calls for multiple candidates
        /// </summary>
        Task<List<VapiCallLog>> InitiateBatchCallsAsync(List<CandidateCallInfo> candidates);

        /// <summary>
        /// Gets call statistics for interviews
        /// </summary>
        Task<VapiCallStatistics> GetCallStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Processes successful interview booking from call
        /// </summary>
        Task<bool> ProcessInterviewBookingAsync(string callId, DateTime interviewDateTime);

        /// <summary>
        /// Sends follow-up after unsuccessful call
        /// </summary>
        Task<bool> SendFollowUpAsync(string callId);

        /// <summary>
        /// Gets candidates pending call-back
        /// </summary>
        Task<List<VapiCallLog>> GetPendingCallbacksAsync();
    }

    /// <summary>
    /// Candidate information for making a call
    /// </summary>
    public class CandidateCallInfo
    {
        public int InterviewId { get; set; }
        public int CandidateId { get; set; }
        public string CandidateName { get; set; }
        public string CandidateEmail { get; set; }
        public string CandidatePhone { get; set; }
        public string JobTitle { get; set; }
        public DateTime? PreferredDateTime { get; set; }
    }

    /// <summary>
    /// Call statistics model
    /// </summary>
    public class VapiCallStatistics
    {
        public int TotalCalls { get; set; }
        public int SuccessfulCalls { get; set; }
        public int FailedCalls { get; set; }
        public int BookingsCompleted { get; set; }
        public int PendingCallbacks { get; set; }
        public double AverageDurationSeconds { get; set; }
        public double TotalCost { get; set; }
        public double BookingConversionRate { get; set; }
        public Dictionary<string, int> CallsByStatus { get; set; }
    }
}
