using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;

namespace Risebird.AzureTimerFunctions
{
    public class AIQualityCheckBatchRun
    {
        private static readonly TimeSpan HttpTimeout = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan DelayBetweenCalls = TimeSpan.FromSeconds(5);

        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IDashboarRepository _dashboardRepository;
        private readonly ILogger log;

        public AIQualityCheckBatchRun(ILoggerFactory loggerFactory,
            IApplicationErrorLogger applicationErrorLogger,
            IDashboarRepository dashboardRepository)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _dashboardRepository = dashboardRepository;
            log = loggerFactory.CreateLogger<AIQualityCheckBatchRun>();
        }

        [Function("AIQualityCheckBatchRun")]
        public async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer)
        {
            log.LogInformation($"AIQualityCheckBatchRun: started at {DateTime.Now}");

            try
            {
                await _dashboardRepository.ResetStuckAIQCAttempts();

                var qcPendingReports = await _dashboardRepository.GetQCPendingReportsForAIQC();
                if (qcPendingReports == null || !qcPendingReports.Any())
                {
                    log.LogInformation("AIQualityCheckBatchRun: no pending reports.");
                    return;
                }

                log.LogInformation($"AIQualityCheckBatchRun: processing {qcPendingReports.Count()} interview(s).");

                foreach (var item in qcPendingReports)
                {
                    var qcSucceeded = false;

                    try
                    {
                        using var client = CreateHttpClient();
                        var qcUrl = $"https://portal.risebird.io/Interview/AIClientReport?interviewId={item.id}&irfId={item.irfIntId}&isOpenAIQC=true&isBackgroudJobRequest=true";
                        var response = await client.GetAsync(qcUrl);
                        response.EnsureSuccessStatusCode();
                        qcSucceeded = true;
                        log.LogInformation($"AIQualityCheckBatchRun: OpenAI QC succeeded for interview {item.id}.");
                    }
                    catch (Exception ex)
                    {
                        log.LogWarning($"AIQualityCheckBatchRun: OpenAI QC failed for interview {item.id}: {ex.Message}");
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        await _dashboardRepository.ResetAIQCAttempted(item.id);
                    }

                    if (!qcSucceeded)
                    {
                        await Task.Delay(DelayBetweenCalls);
                        continue;
                    }

                    await Task.Delay(DelayBetweenCalls);

                    try
                    {
                        using var client = CreateHttpClient();
                        var contradictionUrl = $"https://portal.risebird.io/Interview/AIClientReport?interviewId={item.id}&irfId={item.irfIntId}&IsContradictionAI=true&isBackgroudJobRequest=true";
                        var response = await client.GetAsync(contradictionUrl);
                        response.EnsureSuccessStatusCode();
                        log.LogInformation($"AIQualityCheckBatchRun: Contradiction AI succeeded for interview {item.id}.");
                    }
                    catch (Exception ex)
                    {
                        log.LogWarning($"AIQualityCheckBatchRun: Contradiction AI failed for interview {item.id}: {ex.Message}");
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }

                    await Task.Delay(DelayBetweenCalls);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            log.LogInformation($"AIQualityCheckBatchRun: finished at {DateTime.Now}");
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = HttpTimeout };
            return client;
        }
    }
}
