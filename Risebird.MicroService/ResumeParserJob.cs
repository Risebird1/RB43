using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class ResumeParserJob
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingRespository _meetingRespository;
        private readonly ICandidateRepository _candidateRepository;
        private readonly IAzureStorageHelper _azureStorageHelper;
        //private readonly IResumeParserHelper _resumeParserHelper;
        private readonly IAffIndaResumeParserHelper _affIndaResumeParserHelper;
        private readonly IIRFService _irfService;
        private readonly ISASUrlHelper _sasUrlHelper;
        private readonly IValidationHelper _validationHelper;
        private readonly IAssistantServiceHelper _assistantServiceHelper;

        private readonly ILogger log;

        public ResumeParserJob(ILoggerFactory loggerFactory, IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper,
            IMeetingRespository meetingRespository, ICandidateRepository candidateRepository,
            ISASUrlHelper sasUrlHelper, IIRFService irfService,
            IAffIndaResumeParserHelper affIndaResumeParserHelper, IValidationHelper validationHelper,
            IAssistantServiceHelper assistantServiceHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _meetingRespository = meetingRespository;
            _sasUrlHelper = sasUrlHelper;
            //_resumeParserHelper = resumeParserHelper;
            _candidateRepository = candidateRepository;
            _irfService = irfService;
            _affIndaResumeParserHelper = affIndaResumeParserHelper;
            _validationHelper = validationHelper;
            _assistantServiceHelper = assistantServiceHelper;
            log = loggerFactory.CreateLogger<ResumeParserJob>();
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
                    //string resumeLinkToParse = string.Empty;
                    //if(!resume.ResumeLink.Contains(".pdf"))
                    //{
                    //    resumeLinkToParse = await _azureStorageHelper.DownloadAndConvertFromDocToPdfAsync(resume.ResumeLink);
                    //}
                    //else
                    //{
                    //    resumeLinkToParse = resume.ResumeLink;
                    //}
                    //var response = await _assistantServiceHelper.ScreenAsync(new ScreeningRequest { ResumeLink = resumeLinkToParse, Type = ProfileNames.ResumeParserAIAssistant });

                    // call the api for each of those resumes, return a candidate
                    var candidateData = await _affIndaResumeParserHelper.UploadAndParseResume(resume.ResumeLink, resume.SkillChannelName, resume.IsAffindaIndexCreated);



                    try
                    {
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
                                candidate.EmailId = CandidateEmailNormalizer.Normalize(documentData.data.emails[0]); // null when no trustworthy address: reported as not valid below
                                if (!_validationHelper.ValidateEmailAddress(candidate.EmailId))
                                {
                                    //mark it as cant be processed in the database
                                    candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = false, ReasonBehindNotProcessed = $"Invalid email format: {documentData.data.emails[0]}" });
                                    continue;
                                }

                            }
                            else
                            {
                                //mark it as cant be processed in the database
                                candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = true, ReasonBehindNotProcessed = "Email address not found in resume" });
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
                            candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = false, IsProcessed = false, IsResumeParsingFailed = true, ReasonBehindNotProcessed = "Resume parsing failed - unable to extract data from resume" });

                        }
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        log.LogInformation($" ResumeParserJob : Threw an exception for resume {resume.id}. Message :{ex.Message}, stacktrace : {ex.StackTrace}, source : {ex.Source}");
                        candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = false, IsProcessed = false, IsResumeParsingFailed = true, ReasonBehindNotProcessed = $"Exception during processing: {ex.Message}" });
                        continue;
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
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
