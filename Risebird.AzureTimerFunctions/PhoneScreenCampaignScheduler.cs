using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// Timer that drives automatic phone-screen calling in the MCQ application. Every tick it POSTs
    /// {MCQ_BASE_URL}/api/phone-screen/campaigns/run-due with the X-Scheduler-Key header, and the MCQ
    /// app dials the next round of every Running campaign. This function never picks candidates, never
    /// reads the DB and never talks to ElevenLabs — calling window, attempt caps, concurrency, DNC and
    /// stuck-call recovery are all enforced on the MCQ side.
    ///
    /// Settings:
    ///   PhoneScreenScheduler:BaseUrl — https://screen.risebird.io (production only)
    ///   PhoneScreenScheduler:ApiKey  — must match PhoneScreenScheduler:ApiKey on the MCQ side
    /// Runs every 30 minutes (UTC), at :00 and :30 of every hour.
    ///
    /// A failed run is NOT retried; the next tick is the retry. Azure timer triggers are singleton, so
    /// a run never starts while the previous one is still in progress.
    /// </summary>
    public class PhoneScreenCampaignScheduler
    {
        private const string RunDuePath = "/api/phone-screen/campaigns/run-due";
        private const string KeyHeader = "X-Scheduler-Key";
        private const int AlertAfterConsecutive = 3;
        private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(120);

        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        // Streak counters for the "3 in a row" alerts. They live for the lifetime of the worker
        // process; a restart resets them, which only delays an alert by a few ticks.
        private static int _consecutiveFailures;
        private static int _consecutiveRecoveries;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger log;

        public PhoneScreenCampaignScheduler(ILoggerFactory loggerFactory,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            log = loggerFactory.CreateLogger<PhoneScreenCampaignScheduler>();
        }

        [Function("PhoneScreenCampaignScheduler")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer)
        {
            var baseUrl = _configuration["PhoneScreenScheduler:BaseUrl"]?.Trim().TrimEnd('/');
            var apiKey = _configuration["PhoneScreenScheduler:ApiKey"]?.Trim();

            // No fallback host: a missing base URL must never make a dev scheduler dial production.
            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(apiKey))
            {
                await Alert($"PhoneScreenCampaignScheduler: PhoneScreenScheduler:BaseUrl or PhoneScreenScheduler:ApiKey is not configured; nothing was dialled.");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            HttpStatusCode? status = null;

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = HttpTimeout;

                using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl + RunDuePath)
                {
                    // Empty body so Content-Length: 0 is sent.
                    Content = new ByteArrayContent(Array.Empty<byte>())
                };
                request.Headers.Add(KeyHeader, apiKey);

                using var response = await client.SendAsync(request);
                status = response.StatusCode;
                var body = await response.Content.ReadAsStringAsync();
                stopwatch.Stop();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Automatic calling is stopped for every campaign until the keys match — alert straight away.
                    RegisterFailure();
                    await Alert($"PhoneScreenCampaignScheduler: run-due returned 401 in {stopwatch.ElapsedMilliseconds} ms. " +
                                "X-Scheduler-Key does not match PhoneScreenScheduler:ApiKey on the MCQ side; automatic calling is stopped for all campaigns.");
                    return;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var failures = RegisterFailure();
                    var message = $"PhoneScreenCampaignScheduler: run-due returned {(int)response.StatusCode} in {stopwatch.ElapsedMilliseconds} ms " +
                                  $"({failures} failed run(s) in a row). Waiting for the next tick.";
                    if (response.StatusCode == HttpStatusCode.InternalServerError || failures >= AlertAfterConsecutive)
                        await Alert(message);
                    else
                        log.LogWarning(message);
                    return;
                }

                var summary = JsonSerializer.Deserialize<RunDueResponse>(body, JsonOptions);
                if (summary == null)
                {
                    await HandleFailure($"PhoneScreenCampaignScheduler: run-due returned {(int)response.StatusCode} with an unreadable body in {stopwatch.ElapsedMilliseconds} ms.");
                    return;
                }

                _consecutiveFailures = 0;

                log.LogInformation("PhoneScreenCampaignScheduler: runId={RunId} status={Status} durationMs={DurationMs} campaignsChecked={CampaignsChecked} " +
                                   "callsPlaced={CallsPlaced} callsFailed={CallsFailed} rowsRecovered={RowsRecovered}",
                    summary.RunId, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, summary.CampaignsChecked,
                    summary.CallsPlaced, summary.CallsFailed, summary.RowsRecovered);

                // Only error / not_configured need a person; every other non-ok result is normal.
                foreach (var campaign in summary.Campaigns ?? new List<RunDueCampaignResult>())
                {
                    if (campaign.Result is "error" or "not_configured" or "agent_missing")
                    {
                        log.LogError("PhoneScreenCampaignScheduler: runId={RunId} campaignId={CampaignId} name={Name} irfId={IrfId} result={Result} message={Message}",
                            summary.RunId, campaign.CampaignId, campaign.Name, campaign.IrfId, campaign.Result, campaign.Message);
                        await SafeLogError($"PhoneScreenCampaignScheduler: runId={summary.RunId} campaign #{campaign.CampaignId} '{campaign.Name}' {campaign.Result}: {campaign.Message}");
                    }
                }

                if (summary.RowsRecovered > 0)
                {
                    _consecutiveRecoveries++;
                    if (_consecutiveRecoveries >= AlertAfterConsecutive)
                        await Alert($"PhoneScreenCampaignScheduler: rowsRecovered > 0 on {_consecutiveRecoveries} runs in a row (latest runId={summary.RunId}, " +
                                    $"rowsRecovered={summary.RowsRecovered}). Call results are being lost between ElevenLabs and the MCQ webhook.");
                }
                else
                {
                    _consecutiveRecoveries = 0;
                }
            }
            catch (TaskCanceledException)
            {
                // Timeout. Calls the run already placed are still recorded by the MCQ app; do not retry.
                stopwatch.Stop();
                await HandleFailure($"PhoneScreenCampaignScheduler: run-due timed out after {stopwatch.ElapsedMilliseconds} ms. Waiting for the next tick.");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await HandleFailure($"PhoneScreenCampaignScheduler: run-due failed after {stopwatch.ElapsedMilliseconds} ms " +
                                    $"(status={(status.HasValue ? ((int)status.Value).ToString() : "none")}): {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static int RegisterFailure() => Interlocked.Increment(ref _consecutiveFailures);

        private async Task HandleFailure(string message)
        {
            var failures = RegisterFailure();
            message += $" ({failures} failed run(s) in a row)";
            if (failures >= AlertAfterConsecutive)
                await Alert(message);
            else
                log.LogWarning(message);
        }

        // Never include the scheduler key in anything passed here.
        private async Task Alert(string message)
        {
            log.LogError("[ALERT] " + message);
            await SafeLogError("[ALERT] " + message);
        }

        private async Task SafeLogError(string message)
        {
            try { await _applicationErrorLogger.LogMessage(LogLevelInfo.Error, message); }
            catch (Exception ex) { log.LogWarning($"PhoneScreenCampaignScheduler: failed to write to ApplicationErrorLog: {ex.Message}"); }
        }

        private sealed class RunDueResponse
        {
            public string? RunId { get; set; }
            public DateTime? StartedAtUtc { get; set; }
            public DateTime? FinishedAtUtc { get; set; }
            public int CampaignsChecked { get; set; }
            public int CallsPlaced { get; set; }
            public int CallsFailed { get; set; }
            public int RowsRecovered { get; set; }
            public List<RunDueCampaignResult>? Campaigns { get; set; }
        }

        private sealed class RunDueCampaignResult
        {
            public int CampaignId { get; set; }
            public string? Name { get; set; }
            public int? IrfId { get; set; }
            public string? Result { get; set; }
            public int Dialled { get; set; }
            public int Failed { get; set; }
            public int Recovered { get; set; }
            public string? Message { get; set; }
        }
    }
}
