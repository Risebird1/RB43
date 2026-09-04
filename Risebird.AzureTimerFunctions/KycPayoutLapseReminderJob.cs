using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// Daily Azure Timer Function that reminds interviewers with tracked, KYC-incomplete
    /// payouts (KycPayoutLapseTracking) to complete bank + PAN verification, and lapses
    /// their InterviewPoints for that month if KYC is still incomplete after 21 days.
    /// Runs once per day at 09:30 IST (04:00 UTC), staggered after TdsLapseReminderJob.
    /// </summary>
    public class KycPayoutLapseReminderJob
    {
        private readonly IConfiguration           _configuration;
        private readonly IKycPayoutLapseService   _kycLapseService;
        private readonly INotificationHelper      _notificationHelper;
        private readonly ILogger                  _log;

        // Reminder cadence (day 0, 5, 10, 15, 20) and the hard 5-reminder cap both live on
        // KycPayoutLapseService; this job just enforces the cap defensively before sending.
        private const int DefaultLapseAfterDays = 21;

        public KycPayoutLapseReminderJob(
            ILoggerFactory           loggerFactory,
            IConfiguration           configuration,
            IKycPayoutLapseService   kycLapseService,
            INotificationHelper      notificationHelper)
        {
            _configuration      = configuration;
            _kycLapseService    = kycLapseService;
            _notificationHelper = notificationHelper;
            _log                = loggerFactory.CreateLogger<KycPayoutLapseReminderJob>();
        }

        [Function("KycPayoutLapseReminderJob")]
        public async Task Run([TimerTrigger("0 0 4 * * *")] TimerInfo myTimer)
        {
            _log.LogInformation("KycPayoutLapseReminderJob started at {Time}", DateTime.UtcNow);

            try
            {
                var lapseAfterDays = _configuration.GetValue<int?>("Kyc:LapseReminder:LapseAfterDays") ?? DefaultLapseAfterDays;

                var baseUrl    = _configuration["HostDetails:baseUrl"] ?? "https://portal.risebird.io";
                var verifyPath = _configuration["Tds:Reminder:VerifyPath"] ?? "/Account/UserProfile";
                var kycLink    = $"{baseUrl}{verifyPath}";

                var today   = DateTime.Today;
                var targets = await _kycLapseService.GetReminderTargetsAsync(today);

                _log.LogInformation("KycPayoutLapseReminderJob: found {Count} active tracking rows", targets?.Count ?? 0);

                if (targets == null)
                {
                    _log.LogInformation("KycPayoutLapseReminderJob finished at {Time}", DateTime.UtcNow);
                    return;
                }

                foreach (var target in targets)
                {
                    try
                    {
                        if (target.IsBankVerified && target.IsPanVerified)
                        {
                            await _kycLapseService.MarkCompletedAsync(target.Id);
                            _log.LogInformation("KYC completed in time for user {UserId} month {Month}; tracking row {Id} marked Completed.",
                                target.Userid, target.Month, target.Id);
                            continue;
                        }

                        if (target.DaysInTracking >= lapseAfterDays)
                        {
                            await _kycLapseService.LapseAsync(target.Id, target.Userid, target.Month);
                            _log.LogInformation("Lapsed InterviewPoints for user {UserId} month {Month} (tracking row {Id}) after {Days} days without KYC.",
                                target.Userid, target.Month, target.Id, target.DaysInTracking);
                            continue;
                        }

                        if (target.ReminderCount >= _kycLapseService.MaxReminderCount)
                            continue;

                        var dueIndex = _kycLapseService.GetDueReminderIndex(target.DaysInTracking);
                        if (dueIndex < 0 || target.ReminderCount > dueIndex)
                            continue;

                        var name      = $"{target.FirstName} {target.LastName}".Trim();
                        var lapseDate = target.CreatedOn.AddDays(lapseAfterDays);

                        await _notificationHelper.SendPaymentLapseKYCReminderAsync(
                            target.Email,
                            name,
                            target.PendingAmount,
                            lapseDate,
                            kycLink,
                            dueIndex);

                        await _kycLapseService.MarkReminderSentAsync(target.Id, dueIndex + 1);

                        _log.LogInformation(
                            "Sent KYC lapse reminder #{Index} to {Email} (day {Days}, lapse {LapseDate:yyyy-MM-dd})",
                            dueIndex + 1, target.Email, target.DaysInTracking, lapseDate);
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "Error processing KYC lapse tracking row {Id}", target.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "KycPayoutLapseReminderJob failed");
            }

            _log.LogInformation("KycPayoutLapseReminderJob finished at {Time}", DateTime.UtcNow);
        }
    }
}
