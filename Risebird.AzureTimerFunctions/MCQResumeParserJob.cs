using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
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
    public class MCQResumeParserJob
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingRespository _meetingRespository;
        private readonly ICandidateRepository _candidateRepository;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IAffIndaResumeParserHelper _affIndaResumeParserHelper;
        private readonly IIRFService _irfService;
        private readonly ISASUrlHelper _sasUrlHelper;
        private readonly IValidationHelper _validationHelper;
        private readonly IAssistantServiceHelper _assistantServiceHelper;
        private readonly ILogger log;

        public MCQResumeParserJob(ILoggerFactory loggerFactory, IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper,
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
            _candidateRepository = candidateRepository;
            _irfService = irfService;
            _affIndaResumeParserHelper = affIndaResumeParserHelper;
            _validationHelper = validationHelper;
            _assistantServiceHelper = assistantServiceHelper;
            log = loggerFactory.CreateLogger<MCQResumeParserJob>();
        }

        [Function("MCQResumeParserJob")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            try
            {
                log.LogInformation($" MCQResumeParserJob : Timer trigger function started executing at: {DateTime.Now}");
                //get all the resumes that need to be parsed
                var resumeList = await _candidateRepository.GetCandidateResumesForParsing(ismcqdb: true);
                var candidateList = new List<CandidateQuickAddModel>();
                var candidateResumeList = new List<CandidateResume>();
                foreach (var resume in resumeList)
                {
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
                                candidate.EmailId = documentData.data.emails[0].Trim();
                                candidate.EmailId.Replace(":", "").Replace("-", "").Replace("%20", "").Replace("?:", "").Replace(":%20", "").Replace("-%20", "");
                                if (candidate.EmailId.StartsWith(":") || candidate.EmailId.StartsWith("-"))
                                {
                                    candidate.EmailId = candidate.EmailId.Substring(1); // Remove the first character (colon)
                                }
                                if (candidate.EmailId.StartsWith("%20"))
                                {
                                    candidate.EmailId = candidate.EmailId.Substring(3); // Remove the first three characters 
                                }
                                if (candidate.EmailId.StartsWith("?:"))
                                {
                                    candidate.EmailId = candidate.EmailId.Substring(3); // Remove the first three characters 
                                }
                                if (candidate.EmailId.StartsWith(":%20") || candidate.EmailId.StartsWith("-%20"))
                                {
                                    candidate.EmailId = candidate.EmailId.Substring(4); // Remove the first four characters 
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
                        continue;
                    }
                }
                if (candidateResumeList != null && candidateResumeList.Count() > 0)
                {
                    await _candidateRepository.UpdateCandidateResumeInBulk(candidateResumeList, ismcqdb: true);
                }
                // add the candidate to database and update IsProcessed and IsResumeParsed
                var candidateResumeupdate = new List<CandidateResume>();
                if (candidateList != null && candidateList.Count() > 0)
                {
                    foreach (var irfId in candidateList.Select(x => x.IRFId).Distinct())
                    {
                        var candidateSubList = candidateList.Where(x => x.IRFId == irfId)?.ToList();
                        var result = await _candidateRepository.AddMCQSkillChannelCandidateList(candidateSubList);
                        foreach (var item in result.list)
                        {
                            var id = candidateList.Where(x => x.EmailId == item.emailId && x.IRFId == irfId).Select(x => x.CandidateResumeId).FirstOrDefault();
                            candidateResumeupdate.Add(new CandidateResume { id = id, IsResumeParsed = true, IsProcessed = true, IsResumeParsingFailed = false });
                        }
                    }
                }
                if (candidateResumeupdate != null)
                {
                    await _candidateRepository.UpdateCandidateResumeInBulk(candidateResumeupdate, ismcqdb: true);
                }
                log.LogInformation($" MCQResumeParserJob : Timer trigger function started finished execution at {candidateResumeupdate?.Count()}: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}