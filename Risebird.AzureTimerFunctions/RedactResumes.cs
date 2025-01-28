using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.AzureTimerFunctions
{
    public class RedactResumes
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        //private readonly IMeetingRespository _meetingRespository;
        private readonly IInterviewerRepository _interviewerRepository;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IAffIndaResumeParserHelper _affIndaResumeParserHelper;
        private readonly ISASUrlHelper _sasUrlHelper;
        private readonly ILogger log;

        public RedactResumes(ILoggerFactory loggerFactory, IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper,
            IInterviewerRepository interviewerRepository,
            ISASUrlHelper sasUrlHelper, IAffIndaResumeParserHelper affIndaResumeParserHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            //_meetingRespository = meetingRespository;
            _sasUrlHelper = sasUrlHelper;
            _affIndaResumeParserHelper = affIndaResumeParserHelper;
            _interviewerRepository = interviewerRepository;
            log = loggerFactory.CreateLogger<RedactResumes>();
        }
        [Function("RedactResumes")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)//0 */5 * * * *
        {
            try
            {
                log.LogInformation($" RedactResumes : Timer trigger function started executing at: {DateTime.Now}");

                //get all the resumes that need to be redacted

                var resumeList = await _interviewerRepository.GetInterviewerResumeForRedaction();
                var interviewerList = new List<InterviewerProfileVM>();

                foreach (var item in resumeList)
                {
                    // call the api for each of those resumes, return an interviewer redacted resume link
                    var redactedResumeLink = await _affIndaResumeParserHelper.UploadResumeForRedaction(item.ResumeLink);

                    try
                    {
                        if (!string.IsNullOrEmpty(redactedResumeLink))
                        {
                            var interviewer = new InterviewerProfileVM();

                            HttpClient myWebClient = new HttpClient();
                            string uploadedCodedResumeLink = string.Empty;
                            using (Stream myStream = myWebClient.GetStreamAsync(redactedResumeLink).Result)
                            {
                                var fileName = !string.IsNullOrEmpty(item.FirstName) ? item.FirstName.Replace(" ", "_") : string.Empty;
                                long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                fileName = fileName + "_redacted" + "_" + unixTime.ToString();
                                try
                                {
                                    uploadedCodedResumeLink = _azureStorageHelper.InsertBlobCloudCompare(fileName, "resumes", null, myStream, contentType: "application/pdf").Result;

                                }
                                catch (Exception ex)
                                {
                                    _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                    log.LogError($"Error while inserting coded resume blob  at: {DateTime.Now}, Message: {ex.Message}");
                                    return;
                                }

                                log.LogInformation($"Zoom : File(coded resume) uploaded in blob for {item.FirstName} function executed at: {DateTime.Now}");


                                //generate SAS URL from the uploaded video url
                                var sas = _sasUrlHelper.CreateSasUrl("resumes");

                                if (!string.IsNullOrEmpty(uploadedCodedResumeLink) && !string.IsNullOrEmpty(sas))
                                {
                                    uploadedCodedResumeLink = uploadedCodedResumeLink + "?" + sas;
                                }
                            }

                            interviewer.id = item.id;
                            interviewer.CodedResumeLink = redactedResumeLink;
                            interviewer.IsResumeRedactAttempted = true;
                            interviewer.IsResumeRedactFailed = false;
                            interviewerList.Add(interviewer);
                        }
                        else//parsing was tried, but failed due to some reason, in that case, we wont parse the resume again
                        {
                            interviewerList.Add(new InterviewerProfileVM { id = item.id, IsResumeRedactAttempted = true, IsResumeRedactFailed = true });
                        }
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                        continue;
                    }

                }

                if (interviewerList != null && interviewerList.Count() > 0)
                {
                    await _interviewerRepository.UpdateInterviewerCodedResumeInBulk(interviewerList);
                }

                log.LogInformation($" RedactResumes : Timer trigger function started finished execution at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
