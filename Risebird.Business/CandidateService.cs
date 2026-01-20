using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.Business.Interface;
//using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.RbAPIDataModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Threading.Tasks;
//using static SkiaSharp.HarfBuzz.SKShaper;
//using UglyToad.PdfPig.Logging;
//using Amazon.Rekognition.Model;
using Newtonsoft.Json.Linq;

namespace Risebird.Business
{
    public class CandidateService : ICandidateService
    {
        private readonly ICandidateRepository _candidateRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;
        private readonly IIRFService _irfService;
        private readonly IIRFRepository _irfRepository;
        private readonly ITwilioHelper _twilioHelper;


        private readonly string twilioAuthToken;
        private readonly string twilioAccountSID;
        private readonly string twilioVoiceCallNumber;
        private readonly IEvaluatorRepository _evaluatorRespository;
        private readonly IMeetingRespository _meetingRespository;
        private readonly IOpenAIInterviewerAssistantHelper _ai;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IPdfReaderHelper _pdfReaderHelper;


        public CandidateService(ICandidateRepository candidateRepository, 
            IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration, 
            IIRFService irfService, IIRFRepository irfRepository, ITwilioHelper twilioHelper, 
            IEvaluatorRepository evaluatorRespository, IMeetingRespository meetingRespository, 
            IOpenAIInterviewerAssistantHelper ai, IAzureStorageHelper azureStorageHelper, 
            IPdfReaderHelper pdfReaderHelper)
        {
            _candidateRepository = candidateRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _configuration = configuration;
            _irfService = irfService;
            twilioAuthToken = _configuration["TwilioSettings:AUTH_TOKEN"];
            twilioAccountSID = _configuration["TwilioSettings:ACCOUNT_SID"];
            twilioVoiceCallNumber = _configuration["TwilioSettings:CallerNumber"];
            _irfRepository = irfRepository;
            _twilioHelper = twilioHelper;
            _evaluatorRespository = evaluatorRespository;
            _meetingRespository = meetingRespository;
            _ai = ai;
            _azureStorageHelper = azureStorageHelper;
            _pdfReaderHelper = pdfReaderHelper;
        }

        public async Task<IEnumerable<CandidateTimelineData>> GetCandidateTimeline(string emailId = null)
        {
            try
            {
                return await _candidateRepository.GetCandidateTimeline(emailId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<CandidateTimelineData>> GetCandidateTimelinebyDate(DateTime startDate, DateTime endDate, string emailId = null)
        {
            try
            {
                return await _candidateRepository.GetCandidateTimelinebyDate(startDate, endDate, emailId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IEnumerable<CandidateExcelData>> GetCandidateStatusDataWithoutColdDate(DateTime startDate, DateTime endDate, int clientId)
        {
            try
            {
                return await _candidateRepository.GetCandidateStatusDataWithoutColdDate(startDate, endDate, clientId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IEnumerable<CandidateExcelData>> GetCandidateStatusDataWithColdDate(DateTime startDate, DateTime endDate, int clientId)
        {
            try
            {
                return await _candidateRepository.GetCandidateStatusDataWithColdDate(startDate, endDate, clientId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        //public async Task<bool> AutoCallCandidate(string phoneNumber, int irfId, int candidateId, string loggedInEmail)
        //{
        //    try
        //    {
        //        var link = string.Format("https://portal.risebird.io/TwilioVoice/Index?irfId={0}", irfId.ToString());
        //        var url = new Uri(link);
        //        TwilioClient.Init(twilioAccountSID, twilioAuthToken);
        //        var to = new PhoneNumber(phoneNumber);
        //        var from = new PhoneNumber(twilioVoiceCallNumber);

        //        var call = await CallResource.CreateAsync(
        //            to: to,
        //            from: from,
        //            url: url);

        //        if (call.Status != null)
        //        {
        //            var result = call.Status.ToString();
        //            if (result != CallResource.StatusEnum.Failed.ToString())
        //            {
        //                await _irfService.AddCandidateStatus(loggedInEmail, candidateId, irfId, "Placed a reminder call", (int)CandidateStatusOptions.ReminderCall);
        //                return true;
        //            }
        //            else
        //            {
        //                return false;
        //            }
        //        }
        //        return false;

        //    }

        //    catch (Exception e)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
        //        return false;
        //    }
        //}
        public async Task<bool> AutoCallCandidate(string phoneNumber, int irfId, int candidateId, string loggedInEmail)
        {
            try
            {
                return await _twilioHelper.AutoCallCandidate(phoneNumber, irfId, candidateId, loggedInEmail);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<CandidateDetailsForWhatsApp> FetchInterviewDetails(int interviewid)
        {
            try
            {
                return await _candidateRepository.FetchInterviewDetails(interviewid);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> CancelInterviewDetails(int interviewid, string CandidateEmail)
        {
            try
            {
                return await _candidateRepository.CancelInterviewDetails(interviewid, CandidateEmail);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<InterviewUsersDetails> FetchChannelOwnerandInterviewer(int interviewid)
        {
            try
            {
                return await _candidateRepository.FetchChannelOwnerandInterviewer(interviewid);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<InterviewUsersDetails> FetchInterviewerDetails(int interviewid)
        {
            try
            {
                return await _candidateRepository.FetchInterviewerDetails(interviewid);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<Candidate> GetCandidatesDetails(int irfId, int candidateId)
        {
            try
            {
                return await _candidateRepository.GetCandidatesDetails(irfId, candidateId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public async Task<bool> AddResume(List<CandidateResume> resumeList)
        {
            try
            {
                return await _candidateRepository.AddResume(resumeList);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<IEnumerable<CandidateResume>> GetCandidatesResumes(int irfId, string emailid = null)
        {
            try
            {
                return await _candidateRepository.GetCandidatesResumes(irfId, emailid);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public async Task<bool> DeleteCandidateResume(int id)
        {
            try
            {
                return await _candidateRepository.DeleteCandidateResume(id);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<IEnumerable<string>> GetCandidateStaffingAgencyAndClientHRByInterviewId(int interviewId)
        {
            try
            {
                return await _candidateRepository.GetCandidateStaffingAgencyAndClientHRByInterviewId(interviewId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<CandidateTimelineVM>> GetCandidateTimelinebyDateStatus(DateTime startDate, DateTime endDate, int datatype, string emailId = null)
        {
            try
            {
                return await _candidateRepository.GetCandidateTimelinebyDateStatus(startDate, endDate, datatype, emailId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<FitmentReportModel> GetFitmentReportReport(int candidateId, int irfId)
        {
            try
            {

                var selectabilityReport = await _evaluatorRespository.GetSelectabilityReport(candidateId, irfId);

                //var model = new EvaluatorReport();
                var model = new FitmentReportModel();
                var levels = await _meetingRespository.GetLevels();

                if (selectabilityReport != null && selectabilityReport.Any() /*&& interviewReport.FirstOrDefault().Rating > 0*/)
                {
                    //model.IsEdited = true;
                    model.SelectabilityReportCollection = (IList<SelectabilityEvaluatorReport>)selectabilityReport;

                }
                else
                {
                    // model.IsEdited = false;

                    var selectabilityDetails = await _evaluatorRespository.GetTransSelectability(irfId);
                    var selectabilityList = new List<SelectabilityEvaluatorReport>();
                    foreach (var item in selectabilityDetails)
                    {
                        SelectabilityEvaluatorReport obj = new SelectabilityEvaluatorReport
                        {
                            CandidateId = candidateId,
                            RequiredLevel = levels.FirstOrDefault(x => x.id == item.LevelId)?.Name,
                            RequiredLevelId = levels.FirstOrDefault(x => x.id == item.LevelId).id,
                            Type = item.Type,
                            Name = item.Name,
                            Value = item.Value,
                            WeightageRequired = item.Weightage
                        };
                        selectabilityList.Add(obj);
                    }

                    model.SelectabilityReportCollection = (IList<SelectabilityEvaluatorReport>)selectabilityList;
                }
                var joinabilityReport = await _evaluatorRespository.GetJoinabilityReport(candidateId, irfId);



                if (joinabilityReport != null && joinabilityReport.Any())
                {
                    //model.IsEdited = true;
                    model.JoinabilityReportCollection = (IList<JoinabilityHRReport>)joinabilityReport;

                }
                else
                {
                    //model.IsEdited = false;

                    var joinabilityDetails = await _meetingRespository.GetTransJoinability(irfId);
                    var joinabilityList = new List<JoinabilityHRReport>();
                    foreach (var item in joinabilityDetails)
                    {
                        JoinabilityHRReport obj = new JoinabilityHRReport
                        {
                            CandidateId = candidateId,
                            RequiredLevel = levels.FirstOrDefault(x => x.id == item.LevelId)?.Name,
                            RequiredLevelId = levels.FirstOrDefault(x => x.id == item.LevelId).id,
                            Type = item.Type,
                            Name = item.Name,
                            Value = item.Value,
                            WeightageRequired = item.Weightage
                        };
                        joinabilityList.Add(obj);
                    }

                    model.JoinabilityReportCollection = (IList<JoinabilityHRReport>)joinabilityList;
                }

                if ((joinabilityReport != null && joinabilityReport.Any()) || (selectabilityReport != null && selectabilityReport.Any()))
                {
                    model.IsEdited = true;
                }
                else
                {
                    model.IsEdited = false;
                }

                //get all values from database

                var candidateDetails = await _candidateRepository.GetCandidateForFitmentReport(candidateId, irfId);

                model.CandidateName = candidateDetails.Name;
                model.ResumeLink = candidateDetails.ResumeLink;
                model.IRFId = irfId;
                model.IRFName = candidateDetails.IRFName;
                model.CandidateEmailId = candidateDetails.EmailId;

                model.FitmentStatus = candidateDetails.FitmentStatus;
                model.PriorityStatus = candidateDetails.PriorityStatus;
                model.CompletionStatus = candidateDetails.CompletionStatus;
                model.SelectabilityComments = candidateDetails.SelectabilityComments;
                model.JoinabilityComments = candidateDetails.JoinabilityComments;
                model.Level = new SelectList(levels, "id", "Name");
                return model;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> AddEditFitmentReport(FitmentReportModel model)
        {
            try
            {
                return await _candidateRepository.AddEditFitmentReport(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<bool> ProcessCandidateResume(int id)
        {
            try
            {
                return await _candidateRepository.ProcessCandidateResume(id);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<string> FetchCandidateResumeById(int id)
        {
            try
            {
                return await _candidateRepository.FetchCandidateResumeById(id);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public async Task<string> FetchBillingownerById(int id)
        {
            try
            {
                return await _candidateRepository.FetchBillingownerById(id);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public async Task<InterviewUsersDetails> FetchChannelOwnerByInterviewid(int interviewid)
        {
            try
            {
                return await _candidateRepository.FetchChannelOwnerandInterviewer(interviewid);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IEnumerable<CandidateCommunicationDetailsVM>> GetCandidateCommunicationDetails(int communicationMedium, int? interviewId = null, int? irfId = null, string emailId = null)
        {
            try
            {
                return await _candidateRepository.GetCandidateCommunicationDetails(communicationMedium, interviewId, irfId, emailId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<CandidateIRFDetailsVM> GetIRFDetailsForCandidate(string emailId, int irfId)
        {
            try
            {
                return await _candidateRepository.GetIRFDetailsForCandidate(emailId, irfId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<int> GetuniqueCandidateByEmailId(string emailId, string id)
        {
            try
            {
                return await _candidateRepository.GetuniqueCandidateByEmailId(emailId, id);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return 0;
            }
        }

        public async Task<string> GetCandidateUniqueId(string emailId)
        {
            try
            {
                return await _candidateRepository.GetCandidateUniqueId(emailId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<Candidate> GetCandidateIRFMailDetails(string emailId, int irfId)
        {
            try
            {
                return await _candidateRepository.GetCandidateIRFMailDetails(emailId, irfId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<IRFCandidateReport>> FetchSkillChannelCandidateReprots(int irfId)
        {
            try
            {
                return await _candidateRepository.FetchSkillChannelCandidateReprots(irfId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> SaveCandidateCodedResume(Candidate model)
        {
            try
            {
                return await _candidateRepository.SaveCandidateCodedResume(model);
            }
            catch (SqlTypeException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<List<MeetingVM>> GetColdCandidatesListByClient(string Email)
        {
            try
            {
                return (await _candidateRepository.GetColdCandidatesByClientRepository(Email)).ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<List<CandidateResume>> GetNotProcessedResumeByClient(string Email)
        {
            try
            {
                return (await _candidateRepository.GetNotProcessedResumeByClientRepository(Email)).ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<CandidateExcelData>> GetIRFCandidateStatsForExcelDownLoad(int irfId, string emailid,int projectid)
        {
            try
            {
                return await _candidateRepository.GetIRFCandidateStatsForExcelDownLoad(irfId, emailid, projectid);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> SaveCandidateOpenAIScreeningResults(int irfId, int candidateMasterId, string screeningResponseJson)
        {
            try
            {
                return await _candidateRepository.SaveCandidateOpenAIScreeningResults(irfId, candidateMasterId, screeningResponseJson);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<bool> SaveCandidateOpenAIScreeningResultsV2(int irfId, int candidateMasterId, string screeningResponseJson, string verdict, int resumeScreeningScore)
        {
            try
            {
                return await _candidateRepository.SaveCandidateOpenAIScreeningResultsV2(irfId, candidateMasterId, screeningResponseJson, verdict, resumeScreeningScore);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<Candidate> GetCandidateOpenAIScreeningResults(int irfId, int candidateMasterId)
        {
            try
            {
                return await _candidateRepository.GetCandidateOpenAIScreeningResults(irfId, candidateMasterId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<InterviewChannelDetails>> FetchInterviewChannelCandidateInformation(int irfid, int id, string requestedBy, DateTime? startDate, DateTime? endDate, string jobId)
        {
            try
            {
                return await _candidateRepository.FetchInterviewChannelCandidateInformation(irfid, id, requestedBy, startDate, endDate, jobId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<CandidateTimelineVM>> GetAllCandidateSearchbyDateStatus(DateTime startDate, DateTime endDate, int datatype, string emailId = null)
        {
            try
            {
                return await _candidateRepository.GetAllCandidateSearchbyDateStatus(startDate, endDate, datatype, emailId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<CountryCodeDetails>> GetAllCountryCodeDetails()
        {
            try
            {
                return await _candidateRepository.GetAllCountryCodeDetails();
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> UpdateCandidateAcknowledge(int interviewid, string interviewchecks)
        {
            try
            {
                var result = await _candidateRepository.UpdateCandidateAcknowledge(interviewid, interviewchecks);
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<ResumeSummaryVM> CandidateResumeSummary(int candidateId)
        {
            try
            {
                //check in db if the summary already exists
                ResumeSummaryVM candidateResumeSummary = null;

                var candidate = await _candidateRepository.GetCandidateResumeSummaryById(candidateId);

                if(candidate != null && !string.IsNullOrEmpty(candidate.ResumeLink))
                {
                    string summary = string.Empty;
                    if(string.IsNullOrEmpty(candidate.ResumeSummary))
                    {


                        string resumeLinkToParse = string.Empty;
                        if (!candidate.ResumeLink.Contains(".pdf"))
                        {
                            resumeLinkToParse = await _azureStorageHelper.DownloadAndConvertFromDocToPdfAsync(candidate.ResumeLink);
                        }
                        else
                        {
                            resumeLinkToParse = candidate.ResumeLink;
                        }
                        var resumeStringContent = await _pdfReaderHelper.ReadPdfFromUrlAsync(resumeLinkToParse);

                        if (!string.IsNullOrEmpty(resumeStringContent))
                        {
                            var threadId = await _ai.CreateThreadAsync();
                            await _ai.AddMessageAsync(threadId, "user", resumeStringContent);

                            //await _ai.AddMessageAsync(threadId, "user", resume.ResumeLink);

                            var runId = await _ai.CreateRunAsync(threadId, "asst_pbeYFi03nKIjbl2W1Am0ROQh"); //assistant id of the assistant named : Resume to Profile Summary  
                            await _ai.WaitForRunAsync(threadId, runId);


                            var replies = await _ai.GetMessagesAsync(threadId);

                            try
                            {
                                candidateResumeSummary = TryParseResumeSummary(replies?.FirstOrDefault());
                                if(candidateResumeSummary != null)
                                {
                                    //save the summary to the db
                                    await _candidateRepository.SaveCandidateResumeSummaryAsJsonStringById(candidateId, replies?.FirstOrDefault());
                                }

                            }
                            catch (Exception ex)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                            }
                        }
                        return candidateResumeSummary;
                    }
                    else if(!string.IsNullOrEmpty(candidate.ResumeSummary))
                    {
                        candidateResumeSummary = JsonConvert.DeserializeObject<ResumeSummaryVM>(candidate.ResumeSummary);
                        return candidateResumeSummary;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
                
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        private ResumeSummaryVM TryParseResumeSummary(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var cleaned = raw.Trim();

            // Strip ```json ... ``` wrappers if present
            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned.Substring(7).TrimStart();

            if (cleaned.EndsWith("```"))
                cleaned = cleaned.Substring(0, cleaned.Length - 3).TrimEnd();

            cleaned = cleaned.TrimStart('\n');

            //
            // 1. STRICT DESERIALIZATION
            //
            try
            {
                var strict = JsonConvert.DeserializeObject<ResumeSummaryVM>(cleaned);
                if (strict != null &&
                    (strict.Skills != null ||
                     strict.Work_Experience != null ||
                     strict.Academics != null ||
                     strict.Certifications != null ||
                     !string.IsNullOrEmpty(strict.Confidence)))
                {
                    return strict;
                }
            }
            catch
            {
                // ignore and fall back
            }

            //
            // 2. FALLBACK PARSING (LOOSE MODE)
            //
            try
            {
                var root = JsonConvert.DeserializeObject<JObject>(cleaned);
                if (root == null) return null;

                var result = new ResumeSummaryVM
                {
                    Confidence = root.Value<string>("confidence")
                };

                //
                // Skills Node
                //
                var skillsNode = (JObject)(root["skills"] ?? root["Skills"]);
                if (skillsNode != null)
                {
                    result.Skills = new SkillsSummary
                    {
                        Primary_Specialization = skillsNode.Value<string>("primary_specialization"),
                        Secondary_Specializations = skillsNode["secondary_specializations"] is JArray ss
                            ? ss.Select(x => x.ToString()).ToList()
                            : new List<string>(),
                        Technical_Skills = skillsNode["technical_skills"] is JArray ts
                            ? ts.Select(x => x.ToString()).ToList()
                            : new List<string>(),
                        Soft_Skills = skillsNode["soft_skills"] is JArray so
                            ? so.Select(x => x.ToString()).ToList()
                            : new List<string>()
                    };
                }

                //
                // Work Experience
                //
                var workArr = (JArray)(root["work_experience"] ?? root["workExperience"]);
                if (workArr != null)
                {
                    result.Work_Experience = workArr.Select(w => new WorkExperience
                    {
                        Job_Title = w.Value<string>("job_title"),
                        Company_Name = w.Value<string>("company_name"),
                        Employment_Type = w.Value<string>("employment_type"),
                        Start_Date = w.Value<string>("start_date"),
                        End_Date = w.Value<string>("end_date"),
                        Location = w.Value<string>("location"),
                        Domain_Or_Industry = w.Value<string>("domain_or_industry"),
                        Tech_Stack = w["tech_stack"] is JArray t ? t.Select(x => x.ToString()).ToList() : new List<string>(),
                        Responsibilities_Raw = w["responsibilities_raw"] is JArray r ? r.Select(x => x.ToString()).ToList() : new List<string>()
                    }).ToList();
                }

                //
                // Work Gaps
                //
                var gapsArr = (JArray)(root["work_gaps"] ?? root["workGaps"]);
                if (gapsArr != null)
                {
                    result.Work_Gaps = gapsArr.Select(g => new WorkGap
                    {
                        Gap_Type = g.Value<string>("gap_type"),
                        Start_Date = g.Value<string>("start_date"),
                        End_Date = g.Value<string>("end_date"),
                        Description_Raw = g.Value<string>("description_raw")
                    }).ToList();
                }

                //
                // Academics
                //
                var acadArr = (JArray)(root["academics"] ?? root["education"]);
                if (acadArr != null)
                {
                    result.Academics = acadArr.Select(a => new Academic
                    {
                        Degree = a.Value<string>("degree"),
                        Institution = a.Value<string>("institution"),
                        Field_Of_Study = a.Value<string>("field_of_study"),
                        Start_Date = a.Value<string>("start_date"),
                        End_Date = a.Value<string>("end_date"),
                        Location = a.Value<string>("location"),
                        Achievements = a["achievements"] is JArray ac
                            ? ac.Select(x => x.ToString()).ToList()
                            : new List<string>()
                    }).ToList();
                }

                //
                // Academic Gaps
                //
                var acadGapArr = (JArray)(root["academic_gaps"] ?? root["academicGaps"]);
                if (acadGapArr != null)
                {
                    result.Academic_Gaps = acadGapArr.Select(a => new AcademicGap
                    {
                        Start_Date = a.Value<string>("start_date"),
                        End_Date = a.Value<string>("end_date"),
                        Gap_Reason = a.Value<string>("gap_reason"),
                        Description_Raw = a.Value<string>("description_raw")
                    }).ToList();
                }

                //
                // Certifications
                //
                var certArr = (JArray)(root["certifications"] ?? root["certs"]);
                if (certArr != null)
                {
                    result.Certifications = certArr.Select(c => new Certification
                    {
                        Name = c.Value<string>("name"),
                        Issuing_Organization = c.Value<string>("issuing_organization"),
                        Issue_Date = c.Value<string>("issue_date"),
                        Expiration_Date = c.Value<string>("expiration_date"),
                        Credential_Id = c.Value<string>("credential_id"),
                        Description_Raw = c.Value<string>("description_raw")
                    }).ToList();
                }

                return result;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Candidate> GetDetailsForCandidateResumeScreeningByAI(int irfId, int candidateMasterId)
        {
            try
            {
                return await _candidateRepository.GetDetailsForCandidateResumeScreeningByAI(irfId, candidateMasterId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

    }
}
