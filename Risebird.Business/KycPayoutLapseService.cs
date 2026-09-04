using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class KycPayoutLapseService : IKycPayoutLapseService
    {
        private const int MinAgeDays = 120;

        // Reminders are capped at 5 sends regardless of how many offsets are configured.
        private const int MaxReminderCountValue = 5;
        public int MaxReminderCount => MaxReminderCountValue;

        private readonly IKycPayoutLapseRepository _kycLapseRepo;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly int[] _reminderOffsets;

        public KycPayoutLapseService(
            IKycPayoutLapseRepository kycLapseRepo,
            IApplicationErrorLogger applicationErrorLogger,
            int[] reminderOffsets = null)
        {
            _kycLapseRepo           = kycLapseRepo;
            _applicationErrorLogger = applicationErrorLogger;
            // 5 reminders on day 0, 5, 10, 15, 20 of the tracking window, then lapse on day 21.
            _reminderOffsets        = reminderOffsets ?? new[] { 0, 5, 10, 15, 20 };
        }

        public async Task<int> ProcessLapseCandidatesAsync(string operatorEmail)
        {
            try
            {
                var candidates = await _kycLapseRepo.FetchKycIncompletePayoutCandidatesAsync(DateTime.Today, MinAgeDays);
                if (candidates == null || !candidates.Any())
                    return 0;

                int added = 0;
                foreach (var c in candidates)
                {
                    var remark = $"{c.PendingAmount:0.##} points pending since {c.Month}; KYC incomplete (Bank:{(c.IsBankVerified ? "Verified" : "Missing")}, PAN:{(c.IsPanVerified ? "Verified" : "Missing")}).";
                    var inserted = await _kycLapseRepo.AddLapseTrackingAsync(c.Userid, c.Month, remark, c.PendingAmount, operatorEmail);
                    if (inserted)
                        added++;
                }
                return added;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<IList<KycLapseReminderTarget>> GetReminderTargetsAsync(DateTime asOfDate)
        {
            try
            {
                return await _kycLapseRepo.FetchReminderTargetsAsync(asOfDate);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task MarkCompletedAsync(int id)
        {
            try
            {
                await _kycLapseRepo.UpdateTrackingStatusAsync(id, "Completed");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task MarkReminderSentAsync(int id, int newReminderCount)
        {
            try
            {
                await _kycLapseRepo.MarkReminderSentAsync(id, newReminderCount);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task LapseAsync(int trackingId, int userid, string month)
        {
            try
            {
                await _kycLapseRepo.LapseInterviewPointsAsync(userid, month);
                await _kycLapseRepo.UpdateTrackingStatusAsync(trackingId, "Lapsed");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public int GetDueReminderIndex(int daysElapsed)
        {
            for (int i = _reminderOffsets.Length - 1; i >= 0; i--)
                if (daysElapsed >= _reminderOffsets[i])
                    // Hard cap at MaxReminderCount reminders even if more offsets are configured.
                    return Math.Min(i, MaxReminderCountValue - 1);
            return -1;
        }

        public async Task<IList<KycPayoutLapseTrackingRow>> GetTrackingListAsync()
        {
            try
            {
                return await _kycLapseRepo.FetchTrackingListAsync();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}
