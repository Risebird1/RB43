using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
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
    public class ClonableSkillChannelTemplateService : IClonableSkillChannelTemplateService
    {
        private readonly IIRFRepository _irfRepository;
        private readonly IClonableSkillChannelTemplateRepository _clonableSkillChannelTemplateRepository;
        private readonly IActivityTrackerRepository _activityTrackerRepository;
        //private readonly INotificationHelper _notificationHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;
        private readonly ISkillService _skillService;

        public ClonableSkillChannelTemplateService(IIRFRepository irfRepository, IApplicationErrorLogger applicationErrorLogger,IConfiguration configuration, ISkillService skillService, 
            IClonableSkillChannelTemplateRepository clonableSkillChannelTemplateRepository, IActivityTrackerRepository activityTrackerRepository)
        {
            _irfRepository = irfRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _configuration = configuration;
            _skillService = skillService;
            _clonableSkillChannelTemplateRepository = clonableSkillChannelTemplateRepository;
            _activityTrackerRepository = activityTrackerRepository;
        }
        public async Task<IEnumerable<ClonableSkillChannelVM>> GetAllClonableSkillChannels()
        {
            try
            {
                return await _clonableSkillChannelTemplateRepository.GetAllClonableSkillChannels();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<ClonableSkillChannelVM> GetTemplateDataFromSourceSkillChannel(int irfId, int assessmentGroupId)
        {
            try
            {
                ClonableSkillChannelVM model = new ClonableSkillChannelVM();
                model.id = irfId;
                //get tech skills, general skills, joinability, selectability from the irf
                model.SkillCollection = (IList<TransSkill>)_irfRepository.GetSkillsbyIRFid(irfId);
                model.SkillGeneral = (IList<TransSkillGeneral>)_irfRepository.GetSkillsGeneralbyIRFid(irfId);
                model.Joinability = (IList<TransJoinability>)_irfRepository.GetTransJoinability(irfId);
                model.Selectability = (IList<TransSelectability>)_irfRepository.GetTransSelectability(irfId);

                //pass the assessmentGroupId to get the interviewers and questions list
                model.ClonableTemplateQuestionsCollection = (IList<ClonableTemplateQuestion>)(await _irfRepository.GetQuestionsForAssessmentGroup(assessmentGroupId));

                //populate the dropdowns for recommendation
                model.IRFRecommendations = new IRFRecommendations();

                //model.IRFRecommendations.StatusRadioValue = model.IRFRecommendations.Status == true ? "true" : "false";


                model.IRFRecommendations.AssessmentGroupId = assessmentGroupId != 0 ? assessmentGroupId : 1;

                PopulateDropDowns(model);
                //model.IRFRecommendations.CategoryList = new SelectList(_skillService.GetAssessmentGroupCategory(), "id", "Name");

                //model.IRFRecommendations.CategoryId = _skillService.GetAssessmentGroupCategory(assessmentGroupId).Result;
                //if (model.IRFRecommendations.CategoryId != null && model.IRFRecommendations.CategoryId.HasValue)
                //{
                //    model.IRFRecommendations.AssessmentGroupsList = new SelectList(_skillService.GetAssessmentGroupsByCategory(model.IRFRecommendations.CategoryId.Value).Result, "id", "AssessmentGroup");
                //    model.IRFRecommendations.AssessmentGroupName = model.IRFRecommendations.AssessmentGroupsList?.Where(x => Convert.ToInt16(x.Value) == assessmentGroupId).FirstOrDefault().Text;
                //}


                ////populate the dropdowns for levels, types etc.
                //model.JoinabilityTypeList = new SelectList(_irfRepository.GetJoinabilityMatrix(), "id", "Name");
                //model.SkillGeneralTypeList = new SelectList(_irfRepository.GetGeneralSkillMatrix(), "id", "Name");
                //model.SelectabilityTypeList = new SelectList(_irfRepository.GetSelectabilityMatrix(), "id", "Name");

                //var companyTypes = from CompanyType s in Enum.GetValues(typeof(CompanyType))
                //                   select new { ID = (int)s, Name = s.GetDescription() };
                //model.CompanyTypeList = new SelectList(companyTypes, "ID", "Name");

                //var seniorityLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                //                select new { ID = (int)s, Name = s.GetDescription() };
                //model.SeniorityLevelList = new SelectList(seniorityLevels, "ID", "Name");
                //var competencyLevels = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                //                 select new { ID = (int)s, Name = s.GetDescription() };
                //model.CompetencyLevelList = new SelectList(competencyLevels, "ID", "Name");

                //model.Level = new SelectList(_irfRepository.GetLevels(), "id", "Name");
                //model.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");

                //var Skillset = _irfRepository.GetSkills();
                //if (Skillset != null)
                //{
                //    Skillset = Skillset.OrderBy(x => !String.IsNullOrEmpty(x.Name) ? x.Name.Trim() : "").ToList();
                //}
                //model.Skills = new SelectList(Skillset, "id", "Name");

                
                //return
                return model;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public ClonableSkillChannelVM PopulateDropDowns(ClonableSkillChannelVM model, bool isEdit = false)
        {
            if(!isEdit)
            {
                model.InterviewerCollection = (IList<TransInterviewer>)_irfRepository.GetInterviewersByAssessmentGroup(model.IRFRecommendations.AssessmentGroupId.Value);

                model.IRFRecommendations.CategoryList = new SelectList(_skillService.GetAssessmentGroupCategory(), "id", "Name");

                model.IRFRecommendations.CategoryId = _skillService.GetAssessmentGroupCategory(model.IRFRecommendations.AssessmentGroupId.Value).Result;
                if (model.IRFRecommendations.CategoryId != null && model.IRFRecommendations.CategoryId.HasValue)
                {
                    model.IRFRecommendations.AssessmentGroupsList = new SelectList(_skillService.GetAssessmentGroupsByCategory(model.IRFRecommendations.CategoryId.Value).Result, "id", "AssessmentGroup");
                    model.IRFRecommendations.AssessmentGroupName = model.IRFRecommendations.AssessmentGroupsList?.Where(x => Convert.ToInt16(x.Value) == model.IRFRecommendations.AssessmentGroupId.Value).FirstOrDefault().Text;
                }
            }
            
            //populate the dropdowns for levels, types etc.
            model.JoinabilityTypeList = new SelectList(_irfRepository.GetJoinabilityMatrix(), "id", "Name");
            model.SkillGeneralTypeList = new SelectList(_irfRepository.GetGeneralSkillMatrix(), "id", "Name");
            model.SelectabilityTypeList = new SelectList(_irfRepository.GetSelectabilityMatrix(), "id", "Name");

            var companyTypes = from CompanyType s in Enum.GetValues(typeof(CompanyType))
                               select new { ID = (int)s, Name = s.GetDescription() };
            model.CompanyTypeList = new SelectList(companyTypes, "ID", "Name");

            var seniorityLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                                  select new { ID = (int)s, Name = s.GetDescription() };
            model.SeniorityLevelList = new SelectList(seniorityLevels, "ID", "Name");
            var competencyLevels = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                   select new { ID = (int)s, Name = s.GetDescription() };
            model.CompetencyLevelList = new SelectList(competencyLevels, "ID", "Name");

            model.Level = new SelectList(_irfRepository.GetLevels(), "id", "Name");
            model.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");

            var Skillset = _irfRepository.GetSkills();
            if (Skillset != null)
            {
                Skillset = Skillset.OrderBy(x => !String.IsNullOrEmpty(x.Name) ? x.Name.Trim() : "").ToList();
            }
            model.Skills = new SelectList(Skillset, "id", "Name");
            foreach (var item in model.InterviewerCollection)
            {
                if (item.PerSuccessRate == 0)
                {
                    item.PerSuccessRate = 1000;
                }

            }
            return model;
        }

        public async Task<bool> CreateClonableSkillChannel(ClonableSkillChannelVM model, string loggedInUser)
        {
            try
            {
                if (model.IRFRecommendations != null)
                {
                    model.IRFRecommendations.Status = model.IRFRecommendations.StatusRadioValue == "true" ? true : false;
                }
                model.InterviewerCollection = model.InterviewerCollection?.Where(x => x.IsActive)?.ToList();
                model.ClonableTemplateQuestionsCollection = model.ClonableTemplateQuestionsCollection?.Where(x => x.IsSelected)?.ToList();
                var result = await _clonableSkillChannelTemplateRepository.CreateClonableSkillChannel(model);
                if (result)
                {
                    var activityTrackerModel = new ActivityTrackerVM();
                    activityTrackerModel.ActivityType = (int)ActivityTypes.CreateClonableSkillChannel;
                    activityTrackerModel.Particulars = model.TemplateName;
                    activityTrackerModel.PerformedBy = loggedInUser;
                    activityTrackerModel.Summary = ActivityTypes.CreateClonableSkillChannel.GetDescription();
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

        public async Task<ClonableSkillChannelVM> GetClonableSkillChannel(int id)
        {
            try
            {
                var model = new ClonableSkillChannelVM();
                //get all values from database
                model = await _clonableSkillChannelTemplateRepository.GetClonableSkillChannelById(id);
                var transTechSkills = _irfRepository.GetClonableTechSkillsbyId(id);
                var transSkillsGeneral = _irfRepository.GetClonableIRFSkillsGeneralbyId(id);
                var transJoinability = _irfRepository.GetClonableIRFTransJoinability(id);
                //var transQualityController = _irfRepository.GetTransQualityController(id);
                var transSelectability = _irfRepository.GetClonableIRFTransSelectability(id);
                var templateQuestions = _irfRepository.GetClonableTemplateQuestions(id);
                //var transEmergencyInterviewer = _irfRepository.GetTransEmergencyInterviewer(id);
                model.IRFRecommendations = await _irfRepository.GetIRFRecommendation(id);


                model.IRFRecommendations.CategoryList = new SelectList(_skillService.GetAssessmentGroupCategory(), "id", "Name");
                if (model.IRFRecommendations.CategoryId != null && model.IRFRecommendations.CategoryId.HasValue)
                {
                    model.IRFRecommendations.AssessmentGroupsList = new SelectList(_skillService.GetAssessmentGroupsByCategory(model.IRFRecommendations.CategoryId.Value).Result, "id", "AssessmentGroup");
                    model.IRFRecommendations.AssessmentGroupName = model.IRFRecommendations.AssessmentGroupsList?.Where(x => Convert.ToInt16(x.Value) == model.IRFRecommendations.AssessmentGroupId.Value).FirstOrDefault().Text;
                }

                IEnumerable<ClonableTemplateQuestion> transQuestions = new List<ClonableTemplateQuestion>();
                transQuestions = _irfRepository.GetQuestionsForAssessmentGroup(model.IRFRecommendations.AssessmentGroupId.Value).Result;
                //transQuestions = (IEnumerable<ClonableTemplateQuestion>)transQuestions?.Select(x => ( x.Question, x.SkillId, x.SkillName, x.IsSelected ))?.Distinct();
                foreach (var question in templateQuestions)
                {
                    transQuestions.Where(x => x.Question?.Replace("\t", "").Replace("\n", "").Replace("\r", "") == question.Question?.Replace("\t", "").Replace("\n", "").Replace("\r", "") && x.SkillId == question.SkillId).ToList().ForEach(c => c.IsSelected = true);
                }

                IEnumerable<TransInterviewer> transInterviewer = new List<TransInterviewer>();
                if (model.IRFRecommendations != null && model.IRFRecommendations.AssessmentGroupId > 0)
                {
                    transInterviewer = _irfRepository.GetInterviewersByAssessmentGroup(model.IRFRecommendations.AssessmentGroupId.Value);
                }
                var templateInterviewers = _irfRepository.GetClonableIRFTransInterviewer(id);
                foreach (var interviewer in templateInterviewers)
                {
                    transInterviewer.Where(x => x.UserId == interviewer.UserId).ToList().ForEach(c => c.IsActive = true);
                }

                //assign values retrieved to the model

                model.InterviewerCollection = (IList<TransInterviewer>)(transInterviewer?.OrderByDescending(x => x.IsActive).ToList());
                model.Joinability = (IList<TransJoinability>)(transJoinability);
                model.SkillCollection = (IList<TransSkill>)(transTechSkills);
                model.SkillGeneral = (IList<TransSkillGeneral>)(transSkillsGeneral);
                model.Selectability = (IList<TransSelectability>)(transSelectability);
                model.ClonableTemplateQuestionsCollection = (IList<ClonableTemplateQuestion>)(transQuestions?.OrderByDescending(x => x.IsSelected).ToList());
                if (model.IRFRecommendations != null)
                {
                    model.IRFRecommendations.StatusRadioValue = model.IRFRecommendations.Status == true ? "true" : "false";
                }

                PopulateDropDowns(model, true);

                return model;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> UpdateClonableSkillChannel(ClonableSkillChannelVM model, string loggedInUser)
        {
            try
            {
                if (model.IRFRecommendations != null)
                {
                    model.IRFRecommendations.Status = model.IRFRecommendations.StatusRadioValue == "true" ? true : false;
                }
                model.InterviewerCollection = model.InterviewerCollection?.Where(x => x.IsActive)?.ToList();
                model.ClonableTemplateQuestionsCollection = model.ClonableTemplateQuestionsCollection?.Where(x => x.IsSelected)?.ToList();
                var result = await _clonableSkillChannelTemplateRepository.UpdateClonableSkillChannel(model);
                if (result)
                {
                    var activityTrackerModel = new ActivityTrackerVM();
                    activityTrackerModel.ActivityType = (int)ActivityTypes.UpdateClonableSkillChannel;
                    activityTrackerModel.Particulars = model.TemplateName;
                    activityTrackerModel.PerformedBy = loggedInUser;
                    activityTrackerModel.Summary = ActivityTypes.UpdateClonableSkillChannel.GetDescription();
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
    }
}
