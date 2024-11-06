using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
//using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Threading.Tasks;

namespace Risebird.MicroService
{

    public class CheckInterviewerSkillChannelSuitabilityOpenAI
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IAssistantServiceHelper _assistantServiceHelper;
        private readonly IIRFRepository _irfRepository;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly ILogger log;

        //private readonly string ContainerName;

        public CheckInterviewerSkillChannelSuitabilityOpenAI(ILoggerFactory loggerFactory, IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger,
            IAssistantServiceHelper assistantServiceHelper, IIRFRepository irfRepository,
            IAzureStorageHelper azureStorageHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _assistantServiceHelper = assistantServiceHelper;
            _irfRepository = irfRepository;
            _azureStorageHelper = azureStorageHelper;
            log = loggerFactory.CreateLogger<CheckInterviewerSkillChannelSuitabilityOpenAI>();
        }

        [Function("CheckInterviewerSkillChannelSuitabilityOpenAI")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)//0 */5 * * * * , ILogger log
        {
            //log.LogInformation($"C# Timer trigger function started executing  at: {DateTime.Now}");

            try
            {
                //get transinterviewers for whom screening needs to be done
                var interviewers = await _irfRepository.GetTransInterviewersForOpenAIScreening();



                //Parallel.ForEach(interviewers, item =>
                //{
                //    if (!string.IsNullOrEmpty(item.ResumeLink))
                //    {
                //        var response = _assistantServiceHelper.ScreenAsync(new ScreeningRequest { JDTitle = item.JDTitle, ResumeLink = item.ResumeLink, Type = ProfileNames.Interviewer }).Result;

                //        //save the response to db

                //        var responseInJson = JsonConvert.SerializeObject(response);
                //        bool saveOperationResult = true;
                //        if (!string.IsNullOrEmpty(responseInJson) && responseInJson != "null")
                //        {
                //            saveOperationResult = _irfRepository.SaveTransInterviewerOpenAIScreeningResults(item.id, responseInJson).Result;
                //        }
                //    }
                //});
                foreach (var item in interviewers)
                {


                    //call screening function
                    if (!string.IsNullOrEmpty(item.ResumeLink))
                    {
                        if ((await _azureStorageHelper.IsDocFileAsync(item.ResumeLink)) && string.IsNullOrEmpty(item.ConvertedFromDocResumeLink))
                        {
                            var uploadedResumeUri = await _azureStorageHelper.DownloadAndConvertFromDocToPdfAsync(item.ResumeLink);

                            if (!string.IsNullOrEmpty(uploadedResumeUri))
                            {
                                item.ResumeLink = uploadedResumeUri;
                                //update the interviewers' uri
                                await _irfRepository.SaveInterviewerConvertedFromDocResumeLink(item.id, uploadedResumeUri);
                            }
                        }

                        var response = await _assistantServiceHelper.ScreenAsync(new ScreeningRequest { JDTitle = item.JDTitle, ResumeLink = (item.ResumeLink.EndsWith(".doc") ? item.ConvertedFromDocResumeLink : item.ResumeLink), Type = ProfileNames.Interviewer });

                        //save the response to db
                        if (response != null)
                        {
                            var responseInJson = JsonConvert.SerializeObject(response);
                            bool saveOperationResult = true;
                            if (!string.IsNullOrEmpty(responseInJson) && responseInJson != "null")
                            {
                                saveOperationResult = await _irfRepository.SaveTransInterviewerOpenAIScreeningResults(item.id, responseInJson);
                            }
                        }
                        //else
                        //{
                        //    //check if the resume is a doc file
                        //    if (await _azureStorageHelper.IsDocFileAsync(item.ResumeLink))
                        //    {
                        //        var uploadedResumeUri = await _azureStorageHelper.DownloadAndConvertFromDocToPdfAsync(item.ResumeLink);

                        //        if (!string.IsNullOrEmpty(uploadedResumeUri))
                        //        {
                        //            //update the interviewers' uri
                        //        }
                        //    }
                        //}


                    }

                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            //log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        }
    }
}
