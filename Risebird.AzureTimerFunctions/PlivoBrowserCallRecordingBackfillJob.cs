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
    /// Azure Timer Function — fills recordings for Plivo browser calls (the "Call via: Plivo" option).
    /// Runs every 10 minutes.
    ///
    /// Those calls are logged in CallRecordingData by User/makecall with CallSid = Plivo call UUID.
    /// PlivoVoice/RecordingCallback normally stores the recording URL when the call ends; this job
    /// covers anything it missed (callback not delivered, row logged late) plus calls made before
    /// the callback was wired up, by looking the recording up via the Plivo Recording API
    /// (recording-only — no transcription cost). Looks back 7 days.
    /// </summary>
    public class PlivoBrowserCallRecordingBackfillJob
    {
        private const int MaxAgeDays = 7;
        private const int BatchSize  = 50;

        private readonly IPlivoCXService         _plivoCXService;
        private readonly IUserRepository         _userRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger                 _log;

        public PlivoBrowserCallRecordingBackfillJob(
            ILoggerFactory          loggerFactory,
            IPlivoCXService         plivoCXService,
            IUserRepository         userRepository,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _plivoCXService         = plivoCXService;
            _userRepository         = userRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _log = loggerFactory.CreateLogger<PlivoBrowserCallRecordingBackfillJob>();
        }

        [Function("PlivoBrowserCallRecordingBackfillJob")]
        public async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer)
        {
            _log.LogInformation($"[Plivo] Browser-call recording backfill started at: {DateTime.Now}");

            try
            {
                var pending = await _userRepository.GetPlivoCallsMissingRecordingAsync(MaxAgeDays, BatchSize);
                int filled = 0;

                foreach (var call in pending)
                {
                    try
                    {
                        var info = await _plivoCXService.GetRecordingInfoAsync(call.CallSid);
                        if (string.IsNullOrWhiteSpace(info.RecordingUrl))
                            continue; // no recording (yet) — next run retries while within the window

                        if (await _userRepository.UpdatePlivoRecordingAsync(call.CallSid, info.RecordingUrl, info.DurationSeconds))
                            filled++;
                    }
                    catch (Exception ex)
                    {
                        _log.LogError($"[Plivo] Recording backfill failed for CallRecordingData #{call.id}: {ex.Message}");
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
                }

                _log.LogInformation($"[Plivo] Browser-call recording backfill done — pending: {pending.Count}, filled: {filled}");
            }
            catch (Exception ex)
            {
                _log.LogError($"[Plivo] Browser-call recording backfill error: {ex.Message}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
