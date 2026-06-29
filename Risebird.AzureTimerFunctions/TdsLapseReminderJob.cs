using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// Daily Azure Timer Function that sends wallet-lapse reminder emails to interviewers
    /// who have unclaimed payments and an unverified bank account.
    /// Runs once per day at 09:00 IST (03:30 UTC).
    /// DOES NOT forfeit/lapse anything — reminders only.
    /// </summary>
    public class TdsLapseReminderJob
    {
        private readonly IConfiguration        _configuration;
        private readonly ITdsComplianceService _tdsService;
        private readonly INotificationHelper   _notificationHelper;
        private readonly ILogger               _log;

        // Default reminder day-offsets (configurable via appsettings Tds:Reminder:Offsets)
        private static readonly int[] DefaultOffsets = { 3, 7, 15, 30, 45, 60, 75, 82, 87, 89 };

        public TdsLapseReminderJob(
            ILoggerFactory         loggerFactory,
            IConfiguration         configuration,
            ITdsComplianceService  tdsService,
            INotificationHelper    notificationHelper)
        {
            _configuration      = configuration;
            _tdsService         = tdsService;
            _notificationHelper = notificationHelper;
            _log                = loggerFactory.CreateLogger<TdsLapseReminderJob>();
        }

        [Function("TdsLapseReminderJob")]
        public async Task Run([TimerTrigger("0 30 3 * * *")] TimerInfo myTimer)
        {
            _log.LogInformation("TdsLapseReminderJob started at {Time}", DateTime.UtcNow);

            try
            {
                // Load configurable offsets
                var offsets = _configuration
                    .GetSection("Tds:Reminder:Offsets")
                    .Get<int[]>() ?? DefaultOffsets;

                var baseUrl    = _configuration["HostDetails:baseUrl"] ?? "https://portal.risebird.io";
                var verifyPath = _configuration["Tds:Reminder:VerifyPath"] ?? "/Account/UserProfile";

                var today    = DateTime.Today;
                var targets  = await _tdsService.GetReminderTargetsAsync(today);

                _log.LogInformation("TdsLapseReminderJob: found {Count} unverified-wallet entries", targets.Count);

                foreach (var interviewer in targets)
                {
                    var days = interviewer.DaysSinceFirstUnclaimed;

                    // Find which reminder index is due
                    int dueIndex = -1;
                    for (int i = offsets.Length - 1; i >= 0; i--)
                    {
                        if (days >= offsets[i])
                        {
                            dueIndex = i;
                            break;
                        }
                    }

                    if (dueIndex < 0)
                        continue;

                    var lapseDate  = interviewer.FirstUnclaimedDate.AddDays(90);
                    var verifyLink = $"{baseUrl}{verifyPath}";

                    // LogLapseReminder is idempotent: only inserts if not already sent
                    var shouldSend = await _tdsService.MarkReminderSentAsync(
                        interviewer.Userid,
                        interviewer.PaymentReference,
                        dueIndex,
                        interviewer.Email);

                    if (!shouldSend)
                    {
                        _log.LogDebug(
                            "Skipping duplicate reminder {Index} for user {UserId} ref {Ref}",
                            dueIndex, interviewer.Userid, interviewer.PaymentReference);
                        continue;
                    }

                    var name = $"{interviewer.FirstName} {interviewer.LastName}".Trim();

                    await _notificationHelper.SendTdsLapseReminderAsync(
                        interviewer.Email,
                        name,
                        interviewer.TotalUnpaid,
                        lapseDate,
                        verifyLink,
                        dueIndex);

                    _log.LogInformation(
                        "Sent lapse reminder #{Index} to {Email} (day {Days}, lapse {LapseDate:yyyy-MM-dd})",
                        dueIndex + 1, interviewer.Email, days, lapseDate);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "TdsLapseReminderJob failed");
            }

            _log.LogInformation("TdsLapseReminderJob finished at {Time}", DateTime.UtcNow);
        }
    }
}
