using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class RecommendedIRFService : IRecommendedIRFService
    {
        private readonly IIRFRepository _irfRepository;
        private readonly IRecommendedIRFRepository _recommendedIRFRepository;
        private readonly INotificationHelper _notificationHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IClientService _clientService;
        private readonly IConfiguration _configuration;
        private readonly IActivityTrackerRepository _activityTrackerRepository;

        public RecommendedIRFService(IIRFRepository irfRepository, IApplicationErrorLogger applicationErrorLogger,
            INotificationHelper notificationHelper, IClientService clientService, IConfiguration configuration, IActivityTrackerRepository activityTrackerRepository, IRecommendedIRFRepository recommendedIRFRepository)
        {
            _irfRepository = irfRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _clientService = clientService;
            _configuration = configuration;
            _activityTrackerRepository = activityTrackerRepository;
            _recommendedIRFRepository = recommendedIRFRepository;
        }

        public IEnumerable<SkillCahnnelDetailsVM> GetFetchRecommendedIRF()
        {
            try
            {
                var irfList = _recommendedIRFRepository.GetFetchRecommendedIRF().Result.ToList();
                return irfList;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IRFVm> GetRecommendationIRFByIRFid(int id, bool isClone = false)
        {
            try
            {
                var model = new IRFVm();
                //get all values from database
                var IRF = _recommendedIRFRepository.FetchClonableIRF(id);

                var transTechSkills = _recommendedIRFRepository.GetClonableIRFSkillsbyIRFid(id);
                var transInterviewer = _recommendedIRFRepository.GetClonableIRFTransInterviewer(id);
                var transHR = _recommendedIRFRepository.GetClonableIRFTransHR(id);
                var questionlibrary = _recommendedIRFRepository.GetClonableIRFQuestiondiscussed(id);

                //assign values retrieved to the model
                model.id = IRF.id;
                model.ClientCompName = IRF.ClientCompName;
                model.IRFId = IRF.IRFId;
                model.InterviewerCollection = (IList<TransInterviewer>)(transInterviewer);
                model.QuesionList = (IList<SkillComment>)(questionlibrary);
                model.JDLink = IRF.JDLink;
                model.JDTiltle = IRF.JDTiltle;
                model.JobCountry = IRF.JobCountry;
                model.NoOfIntterview = IRF.NoOfIntterview;
                model.NoOfPosition = IRF.NoOfPosition;
                model.PerMintuteRate = IRF.PerMintuteRate;
                model.RisebirdHr = IRF.RisebirdHr;
                model.CompanyDetails = IRF.CompanyDetails; //Need to Check
                model.SkillCollection = (IList<TransSkill>)(transTechSkills);
                model.StartDate = IRF.StartDate;
                if (!isClone)
                {
                    var transClientUser = _irfRepository.GetTransClientUser(id);
                    model.ClientCollection = (IList<TransClientUser>)(transClientUser);
                }
                model.HRCollection = (IList<TransHR>)(transHR);
                model.ClientId = IRF.ClientId;
                model.ClientName = IRF.ClientName;
                model.CompanyType = IRF.CompanyType;
                model.EndDate = IRF.EndDate;
                model.JobLevel = IRF.JobLevel;
                model.CalendlyLink = IRF.CalendlyLink;
                model.TotalInvites = IRF.TotalInvites;
                model.TotalBudget = IRF.TotalBudget;
                model.IsBiometricEnabled = IRF.IsBiometricEnabled;
                model.PriorityLevel = IRF.PriorityLevel;
                model.Heuristicsscore = IRF.Heuristicsscore;
                model.Type = IRF.BussinessType;
                model.PrimarySkillInvite = IRF.PrimarySkillInvite;
                model.AssessmentGroupId = IRF.AssessmentGroupId;
                model.BillingOwner = IRF.BillingOwner;
                var skillCount = transTechSkills.Count();
                model.OldAssessmentGroupId = model.AssessmentGroupId;
                model.OldCalendlyLink = model.CalendlyLink;
                model.OldJDLink = model.JDLink;
                model.OldInterviewerCollection = model.InterviewerCollection;
                model.OldEmergencyInterviewerCollection = model.EmergencyInterviewerCollection;
                model.OldSkillCollection = model.SkillCollection;
                model.IsQCRequired = IRF.IsQCRequired;
                model.IsJoinabilitySelectabilityRequired = IRF.IsJoinabilitySelectabilityRequired;
                return model;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> AddRecommendationIRFDetails(IRFVm irfDetails, string loggedInUser, string domain)
        {
            try
            {
                irfDetails.IRFId = irfDetails.IRFId.Trim();
                irfDetails.CreatedOn = DateTime.UtcNow;
                int irfId = 0;
                var result = _recommendedIRFRepository.AddRecommendationIRFDetails(irfDetails, loggedInUser, out irfId);
                if (result)
                {
                    var activityTrackerModel = new ActivityTrackerVM();
                    activityTrackerModel.ActivityType = (int)ActivityTypes.AddSkillsChannel;
                    activityTrackerModel.Particulars = irfDetails.IRFId;
                    activityTrackerModel.PerformedBy = loggedInUser;
                    activityTrackerModel.Summary = "Recommendation Skill channel created";
                    if (irfId != 0)
                    {
                        activityTrackerModel.ResourceUrl = domain + $"/IRF/EditIRF?id={irfId}";
                    }
                    await _activityTrackerRepository.AddActivity(activityTrackerModel);
                }
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> UpdateRecommendationIRFDetails(IRFVm irfDetails, string loggedInUser)
        {
            try
            {
                irfDetails.IRFId = irfDetails.IRFId.Trim();
                irfDetails.CreatedOn = DateTime.UtcNow;
                var result = _recommendedIRFRepository.UpdateRecommendationIRFDetails(irfDetails).Result;
                if (result)
                {
                    var activityTrackerModel = new ActivityTrackerVM();
                    activityTrackerModel.ActivityType = (int)ActivityTypes.UpdateSkillsChannel;
                    activityTrackerModel.Particulars = irfDetails.IRFId;
                    activityTrackerModel.PerformedBy = loggedInUser;
                    StringBuilder summary = new StringBuilder();
                    await _activityTrackerRepository.AddActivity(activityTrackerModel);
                }
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public IEnumerable<IRF> GetAllRecommendedIRFs(string emailId)
        {
            try
            {
                var irfList = _recommendedIRFRepository.GetAllRecommendedIRFs(emailId).Result.ToList();
                //foreach (var irf in irfList ?? new List<IRF>())
                //{
                //    if (irf.TotalBudget != 0)
                //    {
                //        var consumedBudget = (irf.TotalBudgetConsumed / irf.TotalBudget) * (100);
                //        irf.BudgetConsumed = consumedBudget.ToString() + "%";
                //    }
                //    else
                //    {
                //        irf.BudgetConsumed = "Not Defined";
                //    }
                //}
                return irfList;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
    }
}
