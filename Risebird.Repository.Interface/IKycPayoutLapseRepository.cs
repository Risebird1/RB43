using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IKycPayoutLapseRepository
    {
        Task<IList<KycLapseCandidate>> FetchKycIncompletePayoutCandidatesAsync(DateTime asOfDate, int minAgeDays);

        /// <summary>
        /// Idempotent insert; returns true if a new row was inserted (false if that
        /// (Userid, Month) is already tracked).
        /// </summary>
        Task<bool> AddLapseTrackingAsync(int userid, string month, string remark, decimal? pendingAmount, string createdBy);

        Task<IList<KycLapseReminderTarget>> FetchReminderTargetsAsync(DateTime asOfDate);

        /// <summary>Transitions an Active tracking row to 'Completed' or 'Lapsed'.</summary>
        Task UpdateTrackingStatusAsync(int id, string status);

        Task MarkReminderSentAsync(int id, int reminderCount);

        Task LapseInterviewPointsAsync(int userid, string month);
    }
}
