using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Threading.Tasks;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// Azure Timer Function — fills recordings for Exotel calls (the "Call via: Exotel" option).
    /// Runs every 10 minutes.
    ///
    /// Those calls are logged in CallRecordingData by User/ExotelConnect with CallSid = Exotel Sid.
    /// TwilioCalls/ExotelStatus normally stores the recording URL when the call ends; Exotel fills
    /// RecordingUrl / Duration asynchronously (~2 minutes after the call), so this job covers anything
    /// the webhook missed by looking the call up via the Exotel call-details API. Looks back 7 days.
    /// </summary>
    public class ExotelCallRecordingBackfillJob
    {
        private const int MaxAgeDays = 7;
        private const int BatchSize  = 50;

        private readonly IExotelService          _exotelService;
        private readonly IUserRepository         _userRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger                 _log;

        public ExotelCallRecordingBackfillJob(
            ILoggerFactory          loggerFactory,
            IExotelService          exotelService,
            IUserRepository         userRepository,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _exotelService          = exotelService;
            _userRepository         = userRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _log = loggerFactory.CreateLogger<ExotelCallRecordingBackfillJob>();
        }

        [Function("ExotelCallRecordingBackfillJob")]
        public async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer)
        {
            _log.LogInformation($"[Exotel] Call recording backfill started at: {DateTime.Now}");

            try
            {
                var pending = await _userRepository.GetExotelCallsMissingRecordingAsync(MaxAgeDays, BatchSize);
                int filled = 0;

                foreach (var call in pending)
                {
                    try
                    {
                        var details = await _exotelService.GetCallDetailsAsync(call.CallSid);
                        if (string.IsNullOrWhiteSpace(details?.RecordingUrl))
                            continue; // no recording (yet) — next run retries while within the window

                        // Store the portal's playback link (Exotel's URL needs the API credentials). Keyed on
                        // CallRecordingData.CallSid — provider-agnostic despite the name.
                        if (await _userRepository.UpdatePlivoRecordingAsync(call.CallSid, ExotelService.RecordingLink(call.CallSid), details.DurationSeconds))
                            filled++;
                    }
                    catch (Exception ex)
                    {
                        _log.LogError($"[Exotel] Recording backfill failed for CallRecordingData #{call.id}: {ex.Message}");
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
                }

                _log.LogInformation($"[Exotel] Call recording backfill done — pending: {pending.Count}, filled: {filled}");
            }
            catch (Exception ex)
            {
                _log.LogError($"[Exotel] Call recording backfill error: {ex.Message}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
