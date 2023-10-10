using Microsoft.Azure.WebJobs;
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

        //private readonly string ContainerName;
        //private readonly string SASConnectionString;

        public ResumeParserJob(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper,
            IMeetingRespository meetingRespository, ICandidateRepository candidateRepository,
            ISASUrlHelper sasUrlHelper, IIRFService irfService,
            IAffIndaResumeParserHelper affIndaResumeParserHelper)
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
            //ContainerName = _configuration["SASURLSettings:ContainerName"];
            //SASConnectionString = _configuration["SASURLSettings:ConnectionString"];


        }
        [FunctionName("ResumeParserJob")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)
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
                    // call the api for each of those resumes, return a candidate
                    var candidateData = await _affIndaResumeParserHelper.UploadAndParseResume(resume.ResumeLink);

                    try
                    {
                        if (!string.IsNullOrEmpty(candidateData))
                        {
                            var documentData = JsonConvert.DeserializeObject<Document>(candidateData);
                            var candidate = new CandidateQuickAddModel();
                            candidate.Name = documentData.data.name.raw;
                            if (documentData.data.emails != null && documentData.data.emails.Count() > 0)
                            {
                                candidate.EmailId = documentData.data.emails[0].Trim();
                                if (candidate.EmailId.StartsWith(":") || candidate.EmailId.StartsWith("-"))
                                {
                                    candidate.EmailId = candidate.EmailId.Substring(1); // Remove the first character (colon)
                                }

                            }
                            else
                            {
                                //mark it as cant be parsed in the database
                                candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false });
                                continue;
                            }
                            if (documentData.data.phoneNumberDetails != null && documentData.data.phoneNumberDetails.Count() > 0)
                            {
                                candidate.Phone = !string.IsNullOrEmpty(documentData.data.phoneNumberDetails[0].internationalCountryCode) ? documentData.data.phoneNumberDetails[0].rawText : null;

                            }
                            candidate.ResumeLink = resume.ResumeLink;
                            candidate.CandidateResumeId = resume.id;
                            candidate.IRFId = resume.IRFId;
                            candidate.AddedByUserId = resume.AddedBy;
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

                if(candidateResumeList !=null && candidateResumeList.Count() >0)
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
