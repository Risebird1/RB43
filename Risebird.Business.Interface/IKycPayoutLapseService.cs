using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IKycPayoutLapseService
    {
        /// <summary>
        /// Scans for KYC-incomplete, 120+ day old Pending/Submitted InterviewPoints and adds
        /// any not-yet-tracked (Interviewer, Month) combinations to KycPayoutLapseTracking.
        /// Returns the count of newly added rows.
        /// </summary>
        Task<int> ProcessLapseCandidatesAsync(string operatorEmail);

        /// <summary>Active tracking rows, re-checked against current KYC verification state.</summary>
        Task<IList<KycLapseReminderTarget>> GetReminderTargetsAsync(DateTime asOfDate);

        /// <summary>Marks a tracking row Completed because KYC was finished in time.</summary>
        Task MarkCompletedAsync(int id);

        /// <summary>Records that a reminder was sent and bumps ReminderCount.</summary>
        Task MarkReminderSentAsync(int id, int newReminderCount);

        /// <summary>Lapses the tracking row and the matching InterviewPoints rows.</summary>
        Task LapseAsync(int trackingId, int userid, string month);

        /// <summary>Returns the zero-based reminder index due for the given days-elapsed, or -1 if none.</summary>
        int GetDueReminderIndex(int daysElapsed);

        /// <summary>Hard cap on how many reminders are ever sent for one tracking row.</summary>
        int MaxReminderCount { get; }
    }
}
