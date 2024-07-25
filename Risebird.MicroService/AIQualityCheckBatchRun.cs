//using System;
//using System.Threading.Tasks;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Azure.WebJobs.Host;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
//using Risebird.CommonHelper.Interface;
//using Risebird.Entities.Data;
//using Risebird.Entities.Enum;
//using Risebird.Repository.Interface;

//namespace Risebird.MicroService
//{
//    public class AIQualityCheckBatchRun
//    {
//        private readonly IConfiguration _configuration;
//        private readonly IApplicationErrorLogger _applicationErrorLogger;
//        private readonly IAssistantServiceHelper _assistantServiceHelper;
//        private readonly IIRFRepository _irfRepository;


//        private readonly string ContainerName;

//        public AIQualityCheckBatchRun(IConfiguration configuration,
//            IApplicationErrorLogger applicationErrorLogger,
//            IAssistantServiceHelper assistantServiceHelper, IIRFRepository irfRepository)
//        {
//            _configuration = configuration;
//            _applicationErrorLogger = applicationErrorLogger;
//            _assistantServiceHelper = assistantServiceHelper;
//            _irfRepository = irfRepository;

//        }

//        [FunctionName("AIQualityCheckBatchRun")]
//        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)
//        {
//            log.LogInformation($"AIQualityCheckBatchRun : C# Timer trigger function started executing  at: {DateTime.Now}");

//            try
//            {
//                //get transinterviewers for whom screening needs to be done
//                var interviewers = await _irfRepository.GetTransInterviewersForOpenAIScreening();

//                //Parallel.ForEach(interviewers, item =>
//                //{
//                //    if (!string.IsNullOrEmpty(item.ResumeLink))
//                //    {
//                //        var response = _assistantServiceHelper.ScreenAsync(new ScreeningRequest { JDTitle = item.JDTitle, ResumeLink = item.ResumeLink, Type = ProfileNames.Interviewer }).Result;

//                //        //save the response to db

//                //        var responseInJson = JsonConvert.SerializeObject(response);
//                //        bool saveOperationResult = true;
//                //        if (!string.IsNullOrEmpty(responseInJson) && responseInJson != "null")
//                //        {
//                //            saveOperationResult = _irfRepository.SaveTransInterviewerOpenAIScreeningResults(item.id, responseInJson).Result;
//                //        }
//                //    }
//                //});
//                foreach (var item in interviewers)
//                {
//                    //call screening function
//                    if (!string.IsNullOrEmpty(item.ResumeLink))
//                    {
//                        var response = await _assistantServiceHelper.ScreenAsync(new ScreeningRequest { JDTitle = item.JDTitle, ResumeLink = item.ResumeLink, Type = ProfileNames.Interviewer });

//                        //save the response to db
//                        if (response != null)
//                        {
//                            var responseInJson = JsonConvert.SerializeObject(response);
//                            bool saveOperationResult = true;
//                            if (!string.IsNullOrEmpty(responseInJson) && responseInJson != "null")
//                            {
//                                saveOperationResult = await _irfRepository.SaveTransInterviewerOpenAIScreeningResults(item.id, responseInJson);
//                            }
//                        }


//                    }

//                }
//            }
//            catch (Exception ex)
//            {
//                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
//            }

//            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

//        }
//    }
//}
