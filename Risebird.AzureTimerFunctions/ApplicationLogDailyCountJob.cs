using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// Pre-aggregates Risebirdlogs into Risebirdlogdailycounts so Operation → Logs index stays fast.
    /// Timer: every hour refreshes today + yesterday; nightly refreshes last 7 UTC days.
    /// HTTP: manual backfill for up to 7 days.
    /// </summary>
    public class ApplicationLogDailyCountJob
    {
        private readonly IApplicationLogDailyCountService _dailyCountService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger _log;

        public ApplicationLogDailyCountJob(
            IApplicationLogDailyCountService dailyCountService,
            IApplicationErrorLogger applicationErrorLogger,
            ILoggerFactory loggerFactory)
        {
            _dailyCountService = dailyCountService;
            _applicationErrorLogger = applicationErrorLogger;
            _log = loggerFactory.CreateLogger<ApplicationLogDailyCountJob>();
        }

        /// <summary>Every hour: refresh today and yesterday UTC counts.</summary>
        [Function("ApplicationLogDailyCountHourly")]
        public async Task RunHourly([TimerTrigger("0 15 * * * *")] TimerInfo myTimer)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                await _dailyCountService.AggregateAndUpsertDayAsync(today);
                await _dailyCountService.AggregateAndUpsertDayAsync(today.AddDays(-1));
                _log.LogInformation("ApplicationLogDailyCountHourly refreshed {Today} and {Yesterday}", today, today.AddDays(-1));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "ApplicationLogDailyCountHourly failed");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        /// <summary>Daily at 01:20 UTC: rebuild last 7 UTC days.</summary>
        [Function("ApplicationLogDailyCountNightly")]
        public async Task RunNightly([TimerTrigger("0 20 1 * * *")] TimerInfo myTimer)
        {
            try
            {
                var end = DateTime.UtcNow.Date;
                var start = end.AddDays(-6);
                var days = await _dailyCountService.AggregateRangeAsync(start, end);
                _log.LogInformation("ApplicationLogDailyCountNightly aggregated {Days} days ({Start}..{End})", days, start, end);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "ApplicationLogDailyCountNightly failed");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        /// <summary>
        /// Manual backfill. Query: days=7 (1-7) optional; defaults to last 7 UTC days including today.
        /// </summary>
        [Function("ApplicationLogDailyCountBackfill")]
        public async Task<HttpResponseData> Backfill(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "ApplicationLogs/DailyCountBackfill")]
            HttpRequestData req)
        {
            var response = req.CreateResponse();
            try
            {
                var query = HttpUtility.ParseQueryString(req.Url.Query);
                var daysText = query["days"];
                var days = 7;
                if (!string.IsNullOrWhiteSpace(daysText) && int.TryParse(daysText, out var parsed))
                    days = Math.Max(1, Math.Min(7, parsed));

                var end = DateTime.UtcNow.Date;
                var start = end.AddDays(-(days - 1));
                var aggregated = await _dailyCountService.AggregateRangeAsync(start, end);

                response.StatusCode = HttpStatusCode.OK;
                await response.WriteStringAsync(
                    $"Aggregated {aggregated} day(s) from {start:yyyy-MM-dd} to {end:yyyy-MM-dd} (UTC) into {Risebird.Entities.ViewModel.ApplicationLogDailyCountEntity.TableName}.");
                return response;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(ex.Message);
                return response;
            }
        }
    }
}
