using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// Clears out the job descriptions and voice recordings left behind by AI screening intakes
    /// that were never completed. Nightly, and off unless AIScreening:BlobPurge:Enabled is set.
    ///
    /// When first enabled it reports rather than deletes - see AIScreening:BlobPurge:DryRun, which
    /// defaults to true. Read one night's candidate list from a real environment before turning
    /// that off: the job descriptions share a container with real candidate resumes.
    /// </summary>
    public class AIScreeningBlobPurgeJob
    {
        private readonly IAIScreeningBlobPurgeService _purgeService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger _log;

        public AIScreeningBlobPurgeJob(
            IAIScreeningBlobPurgeService purgeService,
            IApplicationErrorLogger applicationErrorLogger,
            ILoggerFactory loggerFactory)
        {
            _purgeService = purgeService;
            _applicationErrorLogger = applicationErrorLogger;
            _log = loggerFactory.CreateLogger<AIScreeningBlobPurgeJob>();
        }

        /// <summary>02:40 UTC daily, away from the hourly jobs.</summary>
        [Function("AIScreeningBlobPurge")]
        public async Task Run([TimerTrigger("0 40 2 * * *")] TimerInfo myTimer)
        {
            try
            {
                var result = await _purgeService.PurgeAsync();

                if (result.Skipped != null)
                {
                    _log.LogInformation("AIScreeningBlobPurge did nothing: {Reason}", result.Skipped);
                    return;
                }

                _log.LogInformation(
                    "AIScreeningBlobPurge{DryRun}: examined {Examined}, {Verb} {Deleted}, failed {Failed}.",
                    result.DryRun ? " (dry run)" : string.Empty,
                    result.Examined,
                    result.DryRun ? "would delete" : "deleted",
                    result.Deleted,
                    result.Failed);

                // The candidate list is the point of a dry run, so it has to be readable somewhere.
                // Capped: a first run against a container with years of leftovers should not write
                // a log line nobody can open.
                if (result.Candidates.Count > 0)
                {
                    const int shown = 100;
                    var listed = string.Join(", ", result.Candidates.Take(shown));
                    var more = result.Candidates.Count > shown
                        ? $" (+{result.Candidates.Count - shown} more)"
                        : string.Empty;

                    await _applicationErrorLogger.LogMessage(
                        LogLevelInfo.Info,
                        $"AIScreeningBlobPurge{(result.DryRun ? " (dry run)" : "")} selected: {listed}{more}");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                _log.LogError(ex, "AIScreeningBlobPurge failed.");
            }
        }
    }
}
