using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.AffIndaModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.AzureTimerFunctions
{
    public class ResumeParserJob
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ICandidateRepository _candidateRepository;
        private readonly IAzureStorageHelper _azureStorageHelper;
        //private readonly IResumeParserHelper _resumeParserHelper;
        private readonly IAffIndaResumeParserHelper _affIndaResumeParserHelper;
        private readonly IIRFService _irfService;
        private readonly IValidationHelper _validationHelper;
        private readonly IPdfReaderHelper _pdfReaderHelper;
        private readonly IOpenAIInterviewerAssistantHelper _ai;


        private readonly ILogger log;

        public ResumeParserJob(ILoggerFactory loggerFactory, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper,
             ICandidateRepository candidateRepository,
            IIRFService irfService,
            IAffIndaResumeParserHelper affIndaResumeParserHelper, IValidationHelper validationHelper,
            IPdfReaderHelper pdfReaderHelper,
            IOpenAIInterviewerAssistantHelper ai)
        {           
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper; 
            //_resumeParserHelper = resumeParserHelper;
            _candidateRepository = candidateRepository;
            _irfService = irfService;
            _affIndaResumeParserHelper = affIndaResumeParserHelper;
            _validationHelper = validationHelper;
            log = loggerFactory.CreateLogger<ResumeParserJob>();
            _pdfReaderHelper = pdfReaderHelper;
            _ai = ai;
        }

        [Function("ResumeParserJob")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)//0 */5 * * * *
        {
            try
            {
                log.LogInformation($" ResumeParserJob : Timer trigger function started executing at: {DateTime.Now}");

                //get all the resumes that need to be parsed

                var resumeList = await _candidateRepository.GetCandidateResumesForParsing();
                var candidateList = new List<CandidateQuickAddModel>();
                var candidateResumeList = new List<CandidateResume>();

                foreach (var resume in resumeList)
                {
                    
                    string resumeLinkToParse = string.Empty;
                    if (!resume.ResumeLink.Contains(".pdf"))
                    {
                        resumeLinkToParse = await _azureStorageHelper.DownloadAndConvertFromDocToPdfAsync(resume.ResumeLink);
                    }
                    else
                    {
                        resumeLinkToParse = resume.ResumeLink;
                    }
                    //convert resume into string
                    var resumeStringContent = await _pdfReaderHelper.ReadPdfFromUrlAsync(resumeLinkToParse);
                    if (!string.IsNullOrEmpty(resumeStringContent))
                    {
                        var threadId = await _ai.CreateThreadAsync();
                        //the prompt will be different here. Fetch techrubrics and the table of questions, answers and expected answers. Serialize them, add them to a 
                        await _ai.AddMessageAsync(threadId, "user", resumeStringContent);

                        var runId = await _ai.CreateRunAsync(threadId, "asst_T61hVvnYmOCmvyfKylFULdUk"); //assistant id of the assistant named "Candidate Answer Evaluator"
                        await _ai.WaitForRunAsync(threadId, runId);

                        var replies = await _ai.GetMessagesAsync(threadId);

                        CandidateResumeParsedDataOpenAI candidateParsedData = null;

                        try
                        {
                            candidateParsedData = JsonConvert.DeserializeObject<CandidateResumeParsedDataOpenAI>(replies?.FirstOrDefault());

                        }
                        catch (Exception ex)
                        {
                            log.LogInformation($" ResumeParserJob : Threw an exception Message :{ex.Message}, stacktrace : {ex.StackTrace}, source : {ex.Source}, line 107");

                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                        }


                        //call the api for each of those resumes, return a candidate

                        try
                        {
                            if (candidateParsedData == null)
                            {
                                var candidateData = await _affIndaResumeParserHelper.UploadAndParseResume(resume.ResumeLink, resume.SkillChannelName, resume.IsAffindaIndexCreated);
                                if (!string.IsNullOrEmpty(candidateData.Item1))
                                {
                                    var documentData = JsonConvert.DeserializeObject<Document>(candidateData.Item1);
                                    var candidate = new CandidateQuickAddModel();
                                    candidate.Name = documentData.data?.name?.raw;
                                    if (!string.IsNullOrEmpty(candidateData.Item2))
                                    {
                                        candidate.AffindaResumeIdentifier = candidateData.Item2;
                                    }
                                    if (documentData.data.emails != null && documentData.data.emails.Count() > 0)
                                    {
                                        candidate.EmailId = documentData.data.emails[0].Trim();
                                        candidate.EmailId.Replace(":", "").Replace("-", "").Replace("%20", "").Replace("?:", "").Replace(":%20", "").Replace("-%20", "");

                                        if (candidate.EmailId.StartsWith(":") || candidate.EmailId.StartsWith("-"))
                                        {
                                            candidate.EmailId = candidate.EmailId.Substring(1); // Remove the first character (colon)
                                        }
                                        if (candidate.EmailId.StartsWith("%20"))
                                        {
                                            candidate.EmailId = candidate.EmailId.Substring(3); // Remove the first three characters 
                                                                                                //candidate.EmailId.Replace("%20", "");

                                        }
                                        if (candidate.EmailId.StartsWith("?:"))
                                        {
                                            candidate.EmailId = candidate.EmailId.Substring(3); // Remove the first three characters 
                                                                                                //candidate.EmailId.Replace("?:", "");

                                        }
                                        if (candidate.EmailId.StartsWith(":%20") || candidate.EmailId.StartsWith("-%20"))
                                        {
                                            candidate.EmailId = candidate.EmailId.Substring(4); // Remove the first four characters 
                                                                                                //candidate.EmailId.Replace(":%20", "").Replace("-%20", "");
                                        }
                                        if (!_validationHelper.ValidateEmailAddress(candidate.EmailId))
                                        {
                                            //mark it as cant be processed in the database
                                            candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = false });
                                            continue;
                                        }

                                    }
                                    else
                                    {
                                        //mark it as cant be processed in the database
                                        candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = true });
                                        continue;
                                    }
                                    if (documentData.data.phoneNumberDetails != null && documentData.data.phoneNumberDetails.Count() > 0)
                                    {
                                        //candidate.Phone = !string.IsNullOrEmpty(documentData.data.phoneNumberDetails[0].internationalCountryCode) ? documentData.data.phoneNumberDetails[0].rawText : null;
                                        candidate.Phone = documentData.data.phoneNumberDetails[0].rawText;
                                        if (candidate.Phone == "911" || candidate.Phone == "+911")
                                        {
                                            candidate.Phone = null;
                                        }
                                    }
                                    candidate.ResumeLink = resume.ResumeLink;
                                    candidate.CandidateResumeId = resume.id;
                                    candidate.IRFId = resume.IRFId;
                                    candidate.AddedByUserId = resume.AddedBy;
                                    candidate.ClientHREmail = resume.ClientHREmailId;
                                    candidateList.Add(candidate);
                                }
                                else//parsing was tried, but failed due to some reason, in that case, we wont parse the resume again
                                {
                                    candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = false, IsProcessed = false, IsResumeParsingFailed = true });

                                }
                            }

                            else if(candidateParsedData != null)
                            {
                                var candidate = new CandidateQuickAddModel();
                                candidate.Name = candidateParsedData.name;

                                if (!string.IsNullOrEmpty(candidateParsedData.emailId))
                                {
                                    candidate.EmailId = candidateParsedData.emailId.Trim();
                                    candidate.EmailId.Replace(":", "").Replace("-", "").Replace("%20", "").Replace("?:", "").Replace(":%20", "").Replace("-%20", "");

                                    if (candidate.EmailId.StartsWith(":") || candidate.EmailId.StartsWith("-"))
                                    {
                                        candidate.EmailId = candidate.EmailId.Substring(1); // Remove the first character (colon)
                                    }
                                    if (candidate.EmailId.StartsWith("%20"))
                                    {
                                        candidate.EmailId = candidate.EmailId.Substring(3); // Remove the first three characters 
                                                                                            //candidate.EmailId.Replace("%20", "");

                                    }
                                    if (candidate.EmailId.StartsWith("?:"))
                                    {
                                        candidate.EmailId = candidate.EmailId.Substring(3); // Remove the first three characters 
                                                                                            //candidate.EmailId.Replace("?:", "");

                                    }
                                    if (candidate.EmailId.StartsWith(":%20") || candidate.EmailId.StartsWith("-%20"))
                                    {
                                        candidate.EmailId = candidate.EmailId.Substring(4); // Remove the first four characters 
                                                                                            //candidate.EmailId.Replace(":%20", "").Replace("-%20", "");
                                    }
                                    if (!_validationHelper.ValidateEmailAddress(candidate.EmailId))
                                    {
                                        //mark it as cant be processed in the database
                                        candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = false });
                                        continue;
                                    }

                                }
                                else
                                {
                                    //mark it as cant be processed in the database
                                    candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = true });
                                    continue;
                                }
                                if (!string.IsNullOrEmpty(candidateParsedData.phoneNumber))
                                {
                                    candidate.Phone = candidateParsedData.phoneNumber.Replace(" ", string.Empty);
                                    if (candidate.Phone == "911" || candidate.Phone == "+911")
                                    {
                                        candidate.Phone = null;
                                    }
                                }
                                candidate.ResumeLink = resume.ResumeLink;
                                candidate.CandidateResumeId = resume.id;
                                candidate.IRFId = resume.IRFId;
                                candidate.AddedByUserId = resume.AddedBy;
                                candidate.ClientHREmail = resume.ClientHREmailId;
                                candidateList.Add(candidate);
                            }
                            else//parsing was tried, but failed due to some reason, in that case, we wont parse the resume again
                            {
                                candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = false, IsProcessed = false, IsResumeParsingFailed = true });

                            }
                        }
                        catch (Exception ex)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                            log.LogInformation($" ResumeParserJob : Threw an exception Message :{ex.Message}, stacktrace : {ex.StackTrace}, source : {ex.Source}, line 260");

                            continue;
                        }
                    }
                    else
                    {
                        candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = false, IsProcessed = false, IsResumeParsingFailed = true });

                    }

                }

                if (candidateResumeList != null && candidateResumeList.Count() > 0)
                {
                    await _candidateRepository.UpdateCandidateResumeInBulk(candidateResumeList);
                }

                // add the candidate to database and update IsProcessed and IsResumeParsed
                if (candidateList != null && candidateList.Count() > 0)
                {
                    foreach (var irfId in candidateList.Select(x => x.IRFId).Distinct())
                    {
                        var candidateSubList = candidateList.Where(x => x.IRFId == irfId)?.ToList();
                        await _irfService.AddCandidateUsingResumeParserJob(candidateSubList, irfId);
                    }
                }

                log.LogInformation($" ResumeParserJob : Timer trigger function started finished execution at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                log.LogInformation($" ResumeParserJob : Threw an exception Message :{ex.Message}, stacktrace : {ex.StackTrace}, source : {ex.Source}, line 292");

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
