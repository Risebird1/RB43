using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Threading.Tasks;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// Azure Timer Function — fills in Plivo CX call recordings that were not ready at hangup.
    /// Runs every 5 minutes.
    ///
    /// PlivoCXInterviewCallController.HangupCallback stores the Plivo call UUID and tries to pull
    /// the recording straight away, but the recording finalises about a second after hangup, so
    /// the first attempt can come back empty. This job retries those rows (UUID set, RecordingUrl
    /// NULL, ended within the last 24 hours) using the recording-only lookup, which never calls
    /// the Transcription API and so adds no transcription cost.
    /// </summary>
    public class PlivoCXRecordingBackfillJob
    {
        private const int MaxAgeHours = 24;
        private const int BatchSize   = 50;

        private readonly IPlivoCXService         _plivoCXService;
        private readonly IPlivoCXCallRepository  _repository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger                 _log;

        public PlivoCXRecordingBackfillJob(
            ILoggerFactory          loggerFactory,
            IPlivoCXService         plivoCXService,
            IPlivoCXCallRepository  repository,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _plivoCXService         = plivoCXService;
            _repository             = repository;
            _applicationErrorLogger = applicationErrorLogger;
            _log = loggerFactory.CreateLogger<PlivoCXRecordingBackfillJob>();
        }

        [Function("PlivoCXRecordingBackfillJob")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            _log.LogInformation($"[PlivoCX] Recording backfill started at: {DateTime.Now}");

            try
            {
                var pending = await _repository.GetCallLogsMissingRecordingAsync(MaxAgeHours, BatchSize);
                int filled = 0;

                foreach (var callLog in pending)
                {
                    try
                    {
                        var url = await _plivoCXService.GetRecordingUrlAsync(callLog.PlivoCallUuid);
                        if (string.IsNullOrWhiteSpace(url))
                            continue; // not ready yet — next run will retry

                        callLog.RecordingUrl = url;
                        if (await _repository.UpdateCallLogAsync(callLog))
                            filled++;
                    }
                    catch (Exception ex)
                    {
                        _log.LogError($"[PlivoCX] Recording backfill failed for call log #{callLog.Id}: {ex.Message}");
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
                }

                _log.LogInformation($"[PlivoCX] Recording backfill done — pending: {pending.Count}, filled: {filled}");
            }
            catch (Exception ex)
            {
                _log.LogError($"[PlivoCX] Recording backfill error: {ex.Message}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
