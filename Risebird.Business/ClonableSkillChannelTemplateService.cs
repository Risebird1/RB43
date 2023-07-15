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
        private readonly IClientRepository _clientRepository;

        public ClonableSkillChannelTemplateService(IIRFRepository irfRepository, IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration, ISkillService skillService,
            IClonableSkillChannelTemplateRepository clonableSkillChannelTemplateRepository, IActivityTrackerRepository activityTrackerRepository, IClientRepository clientRepository)
        {
            _irfRepository = irfRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _configuration = configuration;
            _skillService = skillService;
            _clonableSkillChannelTemplateRepository = clonableSkillChannelTemplateRepository;
            _activityTrackerRepository = activityTrackerRepository;
            _clientRepository = clientRepository;
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
        public async Task<ClonableSkillChannelVM> GetTemplateDataFromSourceSkillChannel(int irfId, int assessmentGroupId, bool isNewSkillChannel = false)
        {
            try
            {
                ClonableSkillChannelVM model = new ClonableSkillChannelVM();
                model.id = irfId;
                model.IsNewSkillChannel = isNewSkillChannel;
                //get tech skills, general skills, joinability, selectability from the irf
                model.SkillCollection = (IList<TransSkill>)await _irfRepository.GetSkillsbyIRFid(irfId);
                model.SkillGeneral = (IList<TransSkillGeneral>)_irfRepository.GetSkillsGeneralbyIRFid(irfId);
                model.Joinability = (IList<TransJoinability>)_irfRepository.GetTransJoinability(irfId);
                model.Selectability = (IList<TransSelectability>)_irfRepository.GetTransSelectability(irfId);

                model.ClonableTemplateQuestionsCollection = (IList<ClonableTemplateQuestion>)(await _irfRepository.GetQuestionsForAssessmentGroup(assessmentGroupId));

                model.IRFRecommendations = new IRFRecommendations();

                model.IRFRecommendations.AssessmentGroupId = assessmentGroupId != 0 ? assessmentGroupId : 1;

                var assessmentGroupData = await _irfRepository.GetAssessmentGroupDetails(model.IRFRecommendations.AssessmentGroupId.Value);

                SetSelectListForMultiSelectRecommendationValues(model.IRFRecommendations, assessmentGroupData);

                model.IRFRecommendations.AssessmentGroupName = assessmentGroupData.AssessmentGroup;
                model.IRFRecommendations.CompanyType = assessmentGroupData.CompanyType;
                //model.IRFRecommendations.CompetencyLevelId = assessmentGroupData.CompetencyLevel;
                model.IRFRecommendations.CountryType = assessmentGroupData.Country;
                model.IRFRecommendations.PerHourRate = assessmentGroupData.PerHourRate;
                //model.IRFRecommendations.SeniorityLevelId = assessmentGroupData.ProfileType;


                //if (!string.IsNullOrEmpty(assessmentGroupData.ProfileType))
                //{
                //    model.IRFRecommendations.DifficultyLevelArray = assessmentGroupData.ProfileType.Split(',');
                //}
                //var DifficultyLevelTypes = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                //                           select new { ID = (int)s, Name = s.GetDescription() };
                //var selectListforDifficultyLevel = new List<SelectListItem>();
                //foreach (var item in DifficultyLevelTypes)
                //{
                //    bool selected = false;
                //    if (model.IRFRecommendations.DifficultyLevelArray != null && model.IRFRecommendations.DifficultyLevelArray.Contains(Convert.ToString(item.ID)))
                //    {
                //        selected = true;
                //    }
                //    var selectItem = new SelectListItem { Text = item.Name, Value = Convert.ToString(item.ID), Selected = selected};
                //    selectListforDifficultyLevel.Add(selectItem);
                //}
                //model.IRFRecommendations.DifficultyLevelList = selectListforDifficultyLevel;

                //if (!string.IsNullOrEmpty(assessmentGroupData.SeniorityLevel))
                //{
                //    model.IRFRecommendations.SeniorityLevelArray = assessmentGroupData.SeniorityLevel.Split(',');
                //}
                //var seniorityLevelTypes = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                //                          select new { ID = (int)s, Name = s.GetDescription() };
                //var selectListforSeniorityLevel = new List<SelectListItem>();
                //foreach (var item in seniorityLevelTypes)
                //{
                //    bool selected = false;
                //    if (model.IRFRecommendations.SeniorityLevelArray != null && model.IRFRecommendations.SeniorityLevelArray.Contains(Convert.ToString(item.ID)))
                //    {
                //        selected = true;
                //    }
                //    var selectItem = new SelectListItem { Text = item.Name, Value = Convert.ToString(item.ID), Selected = selected };

                //    selectListforSeniorityLevel.Add(selectItem);
                //}
                //model.IRFRecommendations.SeniorityLevelList = selectListforSeniorityLevel;

                //if (!string.IsNullOrEmpty(assessmentGroupData.EmploymentType))
                //{
                //    model.IRFRecommendations.EmploymentTypeArray = assessmentGroupData.EmploymentType.Split(',');
                //}
                //var employementTypes = from EmploymentTypeOptions s in Enum.GetValues(typeof(EmploymentTypeOptions))
                //                       select new { ID = (int)s, Name = s.GetDescription() };
                //var selectListforEmployementTypes = new List<SelectListItem>();
                //foreach (var item in employementTypes)
                //{
                //    bool selected = false;
                //    if (model.IRFRecommendations.EmploymentTypeArray != null && model.IRFRecommendations.EmploymentTypeArray.Contains(Convert.ToString(item.ID)))
                //    {
                //        selected = true;
                //    }
                //    var selectItem = new SelectListItem { Text = item.Name, Value = Convert.ToString(item.ID), Selected = selected };

                //    selectListforEmployementTypes.Add(selectItem);
                //}
                //model.IRFRecommendations.EmploymentTypeList = selectListforEmployementTypes;

                await PopulateDropDownsForTemplate(model);

                return model;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<ClonableSkillChannelVM> PopulateDropDownsForTemplate(ClonableSkillChannelVM model, bool isEdit = false)
        {
            if (!isEdit)
            {
                model.InterviewerCollection = (IList<TransInterviewer>)_irfRepository.GetInterviewersByAssessmentGroup(model.IRFRecommendations.AssessmentGroupId.Value);

                model.IRFRecommendations.CategoryList = new SelectList(await _skillService.GetAssessmentGroupCategory(), "id", "Name");

                model.IRFRecommendations.CategoryId = await _skillService.GetAssessmentGroupCategory(model.IRFRecommendations.AssessmentGroupId.Value);
                if (model.IRFRecommendations.CategoryId != null && model.IRFRecommendations.CategoryId.HasValue)
                {
                    model.IRFRecommendations.AssessmentGroupsList = new SelectList(await _skillService.GetAssessmentGroupsByCategory(model.IRFRecommendations.CategoryId.Value), "id", "AssessmentGroup");
                    model.IRFRecommendations.AssessmentGroupName = model.IRFRecommendations.AssessmentGroupsList?.Where(x => Convert.ToInt32(x.Value) == model.IRFRecommendations.AssessmentGroupId.Value).FirstOrDefault().Text;
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
            //var competencyLevels = from CompetencyLevel s in Enum.GetValues(typeof(CompetencyLevel))
            //                       select new { ID = (int)s, Name = s.GetDescription() };
            //model.CompetencyLevelList = new SelectList(competencyLevels, "ID", "Name");//Heuristicsscorelevel

            model.Level = new SelectList(await _irfRepository.GetLevels(), "id", "Name");
            model.LevelTech = new SelectList(await _irfRepository.GetLevelTech(), "id", "Name");

            var Skillset = await _irfRepository.GetSkills();
            if (Skillset != null)
            {
                Skillset = Skillset.OrderBy(x => !string.IsNullOrEmpty(x.Name) ? x.Name.Trim() : "").ToList();
            }
            model.Skills = new SelectList(Skillset, "id", "Name");
            if (model.IsNewSkillChannel)
            {
                //get interviewer rate from db
                int interviewerRate = 0;
                interviewerRate = await _irfRepository.GetRenewedInterviewerRate(null, null, model.id);
                foreach (var item in model.InterviewerCollection)
                {
                    item.PerSuccessRate = interviewerRate;
                }
            }
            else
            {
                foreach (var item in model.InterviewerCollection)
                {
                    if (item.PerSuccessRate == 0)
                    {
                        item.PerSuccessRate = 1000;
                    }

                }
            }

            return model;
        }

        public async Task<bool> CreateClonableSkillChannel(ClonableSkillChannelVM model, string loggedInUser)
        {
            try
            {
                model.CreatedBy = loggedInUser;
                if (model.IRFRecommendations != null)
                {
                    model.IRFRecommendations.Status = model.IRFRecommendations.StatusRadioValue == "true" ? true : false;
                }
                model.InterviewerCollection = model.InterviewerCollection?.Where(x => x.IsActive)?.ToList();
                model.ClonableTemplateQuestionsCollection = model.ClonableTemplateQuestionsCollection?.Where(x => x.IsSelected)?.ToList();


                if (model.IRFRecommendations.SeniorityLevelArray != null)
                {
                    model.IRFRecommendations.SeniorityLevelId = string.Join(',', model.IRFRecommendations.SeniorityLevelArray);
                }
                if (model.IRFRecommendations.DifficultyLevelArray != null)
                {
                    model.IRFRecommendations.DifficultyLevel = string.Join(',', model.IRFRecommendations.DifficultyLevelArray);
                }
                if (model.IRFRecommendations.EmploymentTypeArray != null)
                {
                    model.IRFRecommendations.EmploymentType = string.Join(',', model.IRFRecommendations.EmploymentTypeArray);
                }
                if (model.IRFRecommendations.PrimarySkills != null)
                {
                    var primarySkillArray = model.IRFRecommendations.PrimarySkills.Split(';');//?.ToList().ForEach(x => { x = x.Trim()}) ;
                    var newList = new List<string>();

                    foreach (var skill in primarySkillArray)
                    {
                        var trimmedSkill = skill.Trim();
                        if (trimmedSkill != string.Empty)
                        {
                            newList.Add(trimmedSkill);
                        }
                    }
                    model.IRFRecommendations.PrimarySkills = string.Join(';', newList);
                }
                if (model.IRFRecommendations.ClientArray != null)
                {
                    model.IRFRecommendations.Clients = string.Join(',', model.IRFRecommendations.ClientArray);
                }
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


                model.IRFRecommendations.CategoryList = new SelectList(await _skillService.GetAssessmentGroupCategory(), "id", "Name");
                if (model.IRFRecommendations.CategoryId != null && model.IRFRecommendations.CategoryId.HasValue)
                {
                    model.IRFRecommendations.AssessmentGroupsList = new SelectList(await _skillService.GetAssessmentGroupsByCategory(model.IRFRecommendations.CategoryId.Value), "id", "AssessmentGroup");
                    //model.IRFRecommendations.AssessmentGroupName = model.IRFRecommendations.AssessmentGroupsList?.Where(x => Convert.ToInt32(x.Value) == model.IRFRecommendations.AssessmentGroupId.Value).FirstOrDefault().Text;
                }

                SetSelectListForMultiSelectRecommendationValues(model.IRFRecommendations);

                IEnumerable<ClonableTemplateQuestion> transQuestions = new List<ClonableTemplateQuestion>();
                transQuestions = await _irfRepository.GetQuestionsForAssessmentGroup(model.IRFRecommendations.AssessmentGroupId.Value);
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

                await PopulateDropDownsForTemplate(model, true);

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
                model.ModifiedBy = loggedInUser;
                if (model.IRFRecommendations != null)
                {
                    model.IRFRecommendations.Status = model.IRFRecommendations.StatusRadioValue == "true" ? true : false;
                }
                model.InterviewerCollection = model.InterviewerCollection?.Where(x => x.IsActive)?.ToList();
                model.ClonableTemplateQuestionsCollection = model.ClonableTemplateQuestionsCollection?.Where(x => x.IsSelected)?.ToList();

                if (model.IRFRecommendations.SeniorityLevelArray != null)
                {
                    model.IRFRecommendations.SeniorityLevelId = string.Join(',', model.IRFRecommendations.SeniorityLevelArray);
                }
                if (model.IRFRecommendations.DifficultyLevelArray != null)
                {
                    model.IRFRecommendations.DifficultyLevel = string.Join(',', model.IRFRecommendations.DifficultyLevelArray);
                }
                if (model.IRFRecommendations.EmploymentTypeArray != null)
                {
                    model.IRFRecommendations.EmploymentType = string.Join(',', model.IRFRecommendations.EmploymentTypeArray);
                }
                if (model.IRFRecommendations.PrimarySkills != null)
                {
                    var primarySkillArray = model.IRFRecommendations.PrimarySkills.Split(';');//?.ToList().ForEach(x => { x = x.Trim()}) ;
                    var newList = new List<string>();

                    foreach (var skill in primarySkillArray)
                    {
                        var trimmedSkill = skill.Trim();
                        if (trimmedSkill != string.Empty)
                        {
                            newList.Add(trimmedSkill);
                        }
                    }
                    model.IRFRecommendations.PrimarySkills = string.Join(';', newList);
                }
                if (model.IRFRecommendations.ClientArray != null)
                {
                    model.IRFRecommendations.Clients = string.Join(',', model.IRFRecommendations.ClientArray);
                }
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

        public async Task<IRFVm> GetTemplateDataForPopulatingSkillChannel(int id, string clientEmailId = null)
        {
            try
            {
                var model = new IRFVm();
                model.CreatedFromTemplateId = id;

                //get all values from database
                //var templateDetails = await _clonableSkillChannelTemplateRepository.GetClonableSkillChannelById(id);
                //var meetingPlatform = await _clonableSkillChannelTemplateRepository.GetClonableSkillChannelMeetingPlatformById(id);
                var details = await _clonableSkillChannelTemplateRepository.GetClonableSkillChannelDetails(id);

                model.MeetingPlatform = details.MeetingPlatform;
                model.IsAadhaarVerificationRequired = details.IsAadhaarVerificationRequired;
                model.Heuristicsscore = details.Heuristicsscore;
                model.CompanyType = details.CompanyType;
                //if(details.CompanyType == "2")//Service
                //{
                //    model.CompanyType = details.CompanyType;

                //}
                //else
                //{
                //    model.CompanyType = "2";

                //}
                model.JobCountry = details.JobCountry;

                var transTechSkills = _irfRepository.GetClonableTechSkillsbyId(id);
                model.IRFRecommendations = await _irfRepository.GetIRFRecommendation(id);

                if (model.IRFRecommendations != null)
                {
                    model.AssessmentGroupId = model.IRFRecommendations.AssessmentGroupId?.ToString();
                    model.AssessmentGroupName = model.IRFRecommendations.AssessmentGroupName;
                    //model.JobCountry = model.IRFRecommendations.CountryType?.ToString();
                    //model.JobLevel = (int)JobLevels.SE;
                    //model.CompanyType = model.IRFRecommendations.CompanyType?.ToString();
                    //model.Heuristicsscore = model.IRFRecommendations.CompetencyLevelId.HasValue ? model.IRFRecommendations.CompetencyLevelId.Value : model.Heuristicsscore;
                }

                //var templateInterviewers = _irfRepository.GetClonableIRFTransInterviewer(id);

                model.InterviewerCollection = (IList<TransInterviewer>)_irfRepository.GetClonableIRFTransInterviewer(id);
                model.SkillCollection = (IList<TransSkill>)(transTechSkills);

                await PopulateDropDownsForSkillChannel(model, clientEmailId);

                return model;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IRFVm> PopulateDropDownsForSkillChannel(IRFVm model, string clientEmailId = null)
        {
            try
            {
                model.Clients = new SelectList(await _irfRepository.GetClients(), "id", "Name");

                if (string.IsNullOrEmpty(clientEmailId))
                {
                    model.ClientUser = new SelectList(_irfRepository.GetClientUser(Convert.ToInt32(model.Clients.FirstOrDefault().Value)), "id", "EmailId");
                }
                else
                {
                    if (model.ClientId == 0)
                    {
                        model.ClientId = await _clientRepository.GetClientByEmailId(clientEmailId);
                    }
                    model.ClientUser = new SelectList(_irfRepository.GetClientUser(model.ClientId), "id", "EmailId");
                    int clientUserId = _irfRepository.GetUserId(clientEmailId);
                    model.ClientCollection = new List<TransClientUser>();
                    model.ClientCollection.Add(new TransClientUser { ClientUserId = clientUserId, EmailId = clientEmailId, Role = _irfRepository.GetClientUserRole(clientUserId) });
                    model.BillingOwner = clientUserId;
                }
                model.LevelTech = new SelectList(await _irfRepository.GetLevelTech(), "id", "Name");
                model.SkillsList = new SelectList(await _irfRepository.GetSkills(), "id", "Name");
                return model;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public IEnumerable<TransSkill> GetClonableTechSkillsbyId(int id)
        {
            try
            {
                return _irfRepository.GetClonableTechSkillsbyId(id);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public IEnumerable<TransInterviewer> GetClonableIRFTransInterviewer(int id)
        {
            try
            {
                return _irfRepository.GetClonableIRFTransInterviewer(id);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        private IRFRecommendations SetSelectListForMultiSelectRecommendationValues(IRFRecommendations model, AssignmentVM assessmentGroupData = null)
        {
            if (assessmentGroupData != null && !string.IsNullOrEmpty(assessmentGroupData.ProfileType))
            {
                model.DifficultyLevelArray = assessmentGroupData.ProfileType.Split(',');
            }
            else if (!string.IsNullOrEmpty(model.DifficultyLevel))
            {
                model.DifficultyLevelArray = model.DifficultyLevel.Split(',');
            }
            var DifficultyLevelTypes = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                       select new { ID = (int)s, Name = s.GetDescription() };
            var selectListforDifficultyLevel = new List<SelectListItem>();
            foreach (var item in DifficultyLevelTypes)
            {
                bool selected = false;
                if (model.DifficultyLevelArray != null && model.DifficultyLevelArray.Contains(Convert.ToString(item.ID)))
                {
                    selected = true;
                }
                var selectItem = new SelectListItem { Text = item.Name, Value = Convert.ToString(item.ID), Selected = selected };
                selectListforDifficultyLevel.Add(selectItem);
            }
            model.DifficultyLevelList = selectListforDifficultyLevel;

            if (assessmentGroupData != null && !string.IsNullOrEmpty(assessmentGroupData.SeniorityLevel))
            {
                model.SeniorityLevelArray = assessmentGroupData.SeniorityLevel.Split(',');
            }
            else if (!string.IsNullOrEmpty(model.SeniorityLevelId))
            {
                model.SeniorityLevelArray = model.SeniorityLevelId.Split(',');
            }
            var seniorityLevelTypes = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                                      select new { ID = (int)s, Name = s.GetDescription() };
            var selectListforSeniorityLevel = new List<SelectListItem>();
            foreach (var item in seniorityLevelTypes)
            {
                bool selected = false;
                if (model.SeniorityLevelArray != null && model.SeniorityLevelArray.Contains(Convert.ToString(item.ID)))
                {
                    selected = true;
                }
                var selectItem = new SelectListItem { Text = item.Name, Value = Convert.ToString(item.ID), Selected = selected };

                selectListforSeniorityLevel.Add(selectItem);
            }
            model.SeniorityLevelList = selectListforSeniorityLevel;

            if (assessmentGroupData != null && !string.IsNullOrEmpty(assessmentGroupData.EmploymentType))
            {
                model.EmploymentTypeArray = assessmentGroupData.EmploymentType.Split(',');
            }
            else if (!string.IsNullOrEmpty(model.EmploymentType))
            {
                model.EmploymentTypeArray = model.EmploymentType.Split(',');
            }
            var employmentTypes = from EmploymentTypeOptions s in Enum.GetValues(typeof(EmploymentTypeOptions))
                                  select new { ID = (int)s, Name = s.GetDescription() };
            var selectListforEmploymentTypes = new List<SelectListItem>();
            foreach (var item in employmentTypes)
            {
                bool selected = false;
                if (model.EmploymentTypeArray != null && model.EmploymentTypeArray.Contains(Convert.ToString(item.ID)))
                {
                    selected = true;
                }
                var selectItem = new SelectListItem { Text = item.Name, Value = Convert.ToString(item.ID), Selected = selected };

                selectListforEmploymentTypes.Add(selectItem);
            }
            model.EmploymentTypeList = selectListforEmploymentTypes;
            model.ClientList = new SelectList(_clientRepository.GetAllClients().Result, "Name", "Name");
            if (!string.IsNullOrEmpty(model.Clients))
            {
                model.ClientArray = model.Clients.Split(',');
            }
            return model;
        }

        public async Task<ClonableSkillChannelVM> GetClonableSkillChannelMeetingPlatformById(int id)
        {
            try
            {
                return await _clonableSkillChannelTemplateRepository.GetClonableSkillChannelMeetingPlatformById(id);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<ClonableSkillChannelVM> GetClonableTempStatsbyId(int id)
        {
            try
            {
                return _irfRepository.GetClonableTempStatsbyId(id);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
    }
}
