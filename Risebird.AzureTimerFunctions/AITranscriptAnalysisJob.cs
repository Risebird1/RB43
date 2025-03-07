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
        private readonly ILogger log;

        public AITranscriptAnalysisJob(ILoggerFactory loggerFactory, 
            IApplicationErrorLogger applicationErrorLogger, IDashboarRepository dashboardRepository,
            IConfiguration configuration, IAssistantServiceHelper assistantServiceHelper,
            IMeetingService meetingService)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _dashboardRepository = dashboardRepository;
            _configuration = configuration;
            _assistantServiceHelper = assistantServiceHelper;
            _meetingService = meetingService;
            log = loggerFactory.CreateLogger<AITranscriptAnalysisJob>();
        }

        [Function("AITranscriptAnalysisJob")]
        public async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer)
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
                            //ScreeningRequest requestMode
                            //var aiAnalysis = await _assistantServiceHelper.ScreenAsync(new ScreeningRequest { TranscriptLink = item.AudioTranscriptFileLink, Type = ProfileNames.TranscriptAnalysisAIAssistant });
                            string transcriptText;
                            using (HttpClient client = new HttpClient())
                            {
                                transcriptText =  await client.GetStringAsync(item.AudioTranscriptFileLink);
                            }
                            var aiAnalysis = await _assistantServiceHelper.GenerateTranscriptAnalysis(transcriptText);
                            if(aiAnalysis != null)
                            {
                                var model = JsonConvert.DeserializeObject<TranscriptAnalysisSchema>(aiAnalysis.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value);
                                //bool saveOperationResult = true;
                                if (model != null)
                                {
                                    await _meetingService.SaveAITranscriptAnalysis(item.id, model);

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
