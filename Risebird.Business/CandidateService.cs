using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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


        public CandidateService(ICandidateRepository candidateRepository, IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration, IIRFService irfService, IIRFRepository irfRepository, ITwilioHelper twilioHelper, IEvaluatorRepository evaluatorRespository, IMeetingRespository meetingRespository)
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

        public async Task<IEnumerable<CandidateResume>> GetCandidatesResumes(int irfId)
        {
            try
            {
                return await _candidateRepository.GetCandidatesResumes(irfId);
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
        public async Task<IEnumerable<CandidateTimelineData>> GetCandidateTimelinebyDateStatus(DateTime startDate, DateTime endDate, int datatype, string emailId = null)
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
    }
}
