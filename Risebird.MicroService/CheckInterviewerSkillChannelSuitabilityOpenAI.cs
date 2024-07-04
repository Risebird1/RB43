using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Threading.Tasks;
using Risebird.Entities.Data;
using Newtonsoft.Json;

namespace Risebird.MicroService
{
    
    public class CheckInterviewerSkillChannelSuitabilityOpenAI
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IAssistantServiceHelper _assistantServiceHelper;
        private readonly IIRFRepository _irfRepository;


        private readonly string ContainerName;

        public CheckInterviewerSkillChannelSuitabilityOpenAI(IConfiguration configuration, 
            IApplicationErrorLogger applicationErrorLogger, 
            IAssistantServiceHelper assistantServiceHelper, IIRFRepository irfRepository)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _assistantServiceHelper = assistantServiceHelper;
            _irfRepository = irfRepository;

        }

        [FunctionName("CheckInterviewerSkillChannelSuitabilityOpenAI")]
        public async Task Run([TimerTrigger("0 */15 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function started executing  at: {DateTime.Now}");

            try
            {
                //get transinterviewers for whom screening needs to be done
                var interviewers = await _irfRepository.GetTransInterviewersForOpenAIScreening();
                foreach (var item in interviewers)
                {
                    //call screening function
                    var response = await _assistantServiceHelper.ScreenAsync(new ScreeningRequest { JDTitle = item.JDTitle, ResumeLink = item.ResumeLink, Type = ProfileNames.Interviewer});

                    //save the response to db

                    var responseInJson = JsonConvert.SerializeObject(response);
                    bool saveOperationResult = true;
                    if (!string.IsNullOrEmpty(responseInJson))
                    {
                        saveOperationResult = await _irfRepository.SaveTransInterviewerOpenAIScreeningResults(item.id, responseInJson);
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        }
    }
}
