using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
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
        private readonly IResumeOptimizationHelper _resumeOptimizationHelper;


        private readonly ILogger log;

        public ResumeParserJob(ILoggerFactory loggerFactory, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper,
             ICandidateRepository candidateRepository,
            IIRFService irfService,
            IAffIndaResumeParserHelper affIndaResumeParserHelper, IValidationHelper validationHelper,
            IPdfReaderHelper pdfReaderHelper,
            IOpenAIInterviewerAssistantHelper ai,
            IResumeOptimizationHelper resumeOptimizationHelper)
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
            _resumeOptimizationHelper = resumeOptimizationHelper;
        }

        [Function("ResumeParserJob")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer) // Every 10 minutes (was: 5 min - reduced OpenAI costs)
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

                    if (!string.IsNullOrEmpty(resume.ResumeLink))
                    {
                        //undo the following comments that parsed the resume using open ai and initiated the variable candidateParsedData when you iron out the bugs/erratics of open ai and are ready to switch back to it as the first choice resume parser.


                        CandidateResumeParsedDataOpenAI candidateParsedData = null;

                        string resumeLinkToParse = string.Empty;
                        if (!resume.ResumeLink.Contains(".pdf"))
                        {
                            resumeLinkToParse = await _azureStorageHelper.DownloadAndConvertFromDocToPdfAsync(resume.ResumeLink);
                        }
                        else
                        {
                            resumeLinkToParse = resume.ResumeLink;
                        }
                        var resumeStringContent = await _pdfReaderHelper.ReadPdfFromUrlAsync(resumeLinkToParse);

                        if (!string.IsNullOrEmpty(resumeStringContent))
                        {
                            // OPTIMIZATION: Pre-screen resume for validity (saves API calls on corrupted resumes)
                            var (isValid, validationReason) = _resumeOptimizationHelper.ValidateResumeForParsing(resumeStringContent);
                            if (!isValid)
                            {
                                log.LogInformation($"ResumeParserJob: Resume {resume.id} validation failed: {validationReason}");
                                candidateResumeList.Add(new CandidateResume 
                                { 
                                    id = resume.id, 
                                    IsResumeParsed = false, 
                                    IsProcessed = false, 
                                    IsResumeParsingFailed = true,
                                    ReasonBehindNotProcessed = validationReason
                                });
                                continue;
                            }

                            // OPTIMIZATION: Reduce token usage by removing boilerplate and excessive whitespace
                            var estimatedTokensBefore = _resumeOptimizationHelper.EstimateTokenCount(resumeStringContent);
                            resumeStringContent = _resumeOptimizationHelper.OptimizeResumeText(resumeStringContent);
                            var estimatedTokensAfter = _resumeOptimizationHelper.EstimateTokenCount(resumeStringContent);
                            
                            log.LogInformation($"ResumeParserJob: Resume {resume.id} optimization: {estimatedTokensBefore} tokens -> {estimatedTokensAfter} tokens (saved {estimatedTokensBefore - estimatedTokensAfter})");

                            var threadId = await _ai.CreateThreadAsync();
                            await _ai.AddMessageAsync(threadId, "user", resumeStringContent);

                            //await _ai.AddMessageAsync(threadId, "user", resume.ResumeLink);

                            var runId = await _ai.CreateRunAsync(threadId, "asst_T61hVvnYmOCmvyfKylFULdUk"); //assistant id of the assistant named (Copy of Resume Parser - Using Resume Public URL) asst_x4F8slGF4hnEI4EN7pBDGR2g 
                            await _ai.WaitForRunAsync(threadId, runId);


                            var replies = await _ai.GetMessagesAsync(threadId);

                            try
                            {
                                candidateParsedData = JsonConvert.DeserializeObject<CandidateResumeParsedDataOpenAI>(replies?.FirstOrDefault());

                            }
                            catch (Exception ex)
                            {
                                log.LogInformation($" ResumeParserJob : Threw an exception Message :{ex.Message}, stacktrace : {ex.StackTrace}, source : {ex.Source}, line 107");

                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                            }
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
                                            candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = false, ReasonBehindNotProcessed = "The extracted(parsed) email is not a valid EmailId." });
                                            continue;
                                        }

                                    }
                                    else
                                    {
                                        //mark it as cant be processed in the database
                                        candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = true, ReasonBehindNotProcessed= "The email could not be extracted after parsing." });
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
                                    candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = false, IsProcessed = false, IsResumeParsingFailed = true, ReasonBehindNotProcessed= "Parsing was tried, but failed due to some reason (third party api failed to parse it), in that case, we wont parse the resume again." });

                                }
                            }

                            else if (candidateParsedData != null)
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
                                        candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = false, ReasonBehindNotProcessed = "The extracted(parsed) email is not a valid EmailId." });
                                        continue;
                                    }

                                }
                                else
                                {
                                    //mark it as cant be processed in the database
                                    candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = true, ReasonBehindNotProcessed = "EmailId is missing." });
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
                                candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = false, IsProcessed = false, IsResumeParsingFailed = true, ReasonBehindNotProcessed = "Parsing was tried, but failed due to some reason (third party api failed to parse it), in that case, we wont parse the resume again." });

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
                        candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = false, IsProcessed = false, IsResumeParsingFailed = true, ReasonBehindNotProcessed = "Resume Link was missing at the time the parsing was attempted." });

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
