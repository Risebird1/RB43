using System.Net.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.OpenAIModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository;
using Risebird.Repository.Interface;
using System;
using static SkiaSharp.HarfBuzz.SKShaper;

namespace Risebird.AzureTimerFunctions
{
    public class AITranscriptAnalysisJob
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        private readonly IConfiguration _configuration;
        private readonly IDashboarRepository _dashboardRepository;
        private readonly IAssistantServiceHelper _assistantServiceHelper;
        private readonly IMeetingService _meetingService;
        private readonly ITranscriptOptimizationHelper _transcriptOptimizationHelper;
        private readonly ILogger log;

        private readonly IHttpClientFactory _httpClientFactory;

        public AITranscriptAnalysisJob(ILoggerFactory loggerFactory,
            IApplicationErrorLogger applicationErrorLogger, IDashboarRepository dashboardRepository,
            IConfiguration configuration, IAssistantServiceHelper assistantServiceHelper,
            IMeetingService meetingService,
            ITranscriptOptimizationHelper transcriptOptimizationHelper, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _applicationErrorLogger = applicationErrorLogger;
            _dashboardRepository = dashboardRepository;
            _configuration = configuration;
            _assistantServiceHelper = assistantServiceHelper;
            _meetingService = meetingService;
            _transcriptOptimizationHelper = transcriptOptimizationHelper;
            log = loggerFactory.CreateLogger<AITranscriptAnalysisJob>();
        }

        [Function("AITranscriptAnalysisJob")]
        public async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer) // Every 10 minutes (reduced OpenAI costs)
        {
            log.LogInformation($"AITranscriptAnalysisJob : C# Timer trigger function started executing  at: {DateTime.Now}");

            try
            {
                var pendingTranscriptAnalysis = await _dashboardRepository.GetPendingAITranscriptAnalysis();
                if (pendingTranscriptAnalysis != null && pendingTranscriptAnalysis.Count() > 0)
                {
                    foreach (var item in pendingTranscriptAnalysis)
                    {
                        try
                        {
                            // OPTIMIZATION: Validate transcript link before attempting download
                            if (string.IsNullOrEmpty(item.AudioTranscriptFileLink))
                            {
                                log.LogInformation($"AITranscriptAnalysisJob: Transcript {item.id} validation failed: Missing transcript link");
                                // Mark as failed - could add a method to update status if needed
                                continue;
                            }

                            //ScreeningRequest requestMode
                            //var aiAnalysis = await _assistantServiceHelper.ScreenAsync(new ScreeningRequest { TranscriptLink = item.AudioTranscriptFileLink, Type = ProfileNames.TranscriptAnalysisAIAssistant });
                            string transcriptText;
                            using (HttpClient client = _httpClientFactory.CreateClient())
                            {
                                transcriptText = await client.GetStringAsync(item.AudioTranscriptFileLink);
                            }

                            // OPTIMIZATION: Pre-screen transcript for validity (saves API calls on corrupted/empty transcripts)
                            if (!string.IsNullOrEmpty(transcriptText))
                            {
                                var (isValid, validationReason) = _transcriptOptimizationHelper.ValidateTranscriptForAnalysis(transcriptText);
                                if (!isValid)
                                {
                                    log.LogInformation($"AITranscriptAnalysisJob: Transcript {item.id} validation failed: {validationReason}");
                                    // Mark as failed - could add a method to update status if needed
                                    continue;
                                }

                                // OPTIMIZATION: Reduce token usage by removing filler words, timestamps, and excessive whitespace
                                var estimatedTokensBefore = _transcriptOptimizationHelper.EstimateTokenCount(transcriptText);
                                transcriptText = _transcriptOptimizationHelper.OptimizeTranscriptText(transcriptText);
                                var estimatedTokensAfter = _transcriptOptimizationHelper.EstimateTokenCount(transcriptText);

                                log.LogInformation($"AITranscriptAnalysisJob: Transcript {item.id} optimization: {estimatedTokensBefore} tokens -> {estimatedTokensAfter} tokens (saved {estimatedTokensBefore - estimatedTokensAfter})");
                            }
                            else
                            {
                                log.LogInformation($"AITranscriptAnalysisJob: Transcript {item.id} is empty or could not be downloaded");
                                continue;
                            }

                            var aiAnalysis = await _assistantServiceHelper.GenerateTranscriptAnalysis(transcriptText);
                            if (aiAnalysis != null)
                            {
                                var model = JsonConvert.DeserializeObject<TranscriptAnalysisSchema>(aiAnalysis.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value);
                                //bool saveOperationResult = true;
                                if (model != null)
                                {
                                    await _meetingService.SaveAITranscriptAnalysis(item.id, model);
                                }
                                try
                                {
                                    var jsonToProcess = aiAnalysis.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value.Replace("```json", "").Replace("```", "");
                                    var modelset = JsonConvert.DeserializeObject<TranscriptAnalysisSchema>(jsonToProcess);
                                    //TransScript Analysis save to db
                                    bool serviceResult = await _meetingService.SaveQcInterviewAIevaluation(item.id, modelset, jsonToProcess, true);
                                }
                                catch (Exception ex)
                                {

                                }
                            }
                            //save the ai analysis into db;

                            //save the response in db
                            //bool saveOperationResult = true;
                            //if (!string.IsNullOrEmpty(responseInJson) && responseInJson != "null")
                            //{
                            //    //saveOperationResult = 
                            //    await _meetingService.SaveAITranscriptAnalysis(item.id, responseInJson);
                            //}
                        }
                        catch (Exception ex)
                        {
                            log.LogInformation($"AITranscriptAnalysisJob :  C# Timer trigger {ex.Message}");
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        }

                    }


                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            log.LogInformation($"AITranscriptAnalysisJob : C# Timer trigger function executed at: {DateTime.Now}");

        }
    }
}
