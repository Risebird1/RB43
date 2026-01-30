using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.OpenAIModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;


namespace Risebird.AzureTimerFunctions
{
    public class RenewTemplateTechRubricsJob
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IClonableSkillChannelTemplateRepository _cscRepository;
        private readonly IAssistantServiceHelper _assistantServiceHelper;
        private readonly ILogger log;

        public RenewTemplateTechRubricsJob(ILoggerFactory loggerFactory, IApplicationErrorLogger applicationErrorLogger,
            IClonableSkillChannelTemplateRepository cscRepository, IAssistantServiceHelper assistantServiceHelper)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _cscRepository = cscRepository;
            _assistantServiceHelper = assistantServiceHelper;
            log = loggerFactory.CreateLogger<RenewTemplateTechRubricsJob>();
        }

        [Function("RenewTemplateTechRubricsJob")]
        public async Task Run([TimerTrigger("0 0 */6 * * *")] TimerInfo myTimer) // Every 6 hours (was: every minute - causing excessive OpenAI costs)
        {
            log.LogInformation($"RenewTemplateTechRubricsJob : C# Timer trigger function started executing  at: {DateTime.Now}");

            try
            {
                var templates = await _cscRepository.GetAllClonableSkillChannelsToBeRenewed();
                if (templates != null && templates.Count() > 0)
                {
                    foreach (var item in templates)
                    {
                        try
                        {
                            var result = await _assistantServiceHelper.GenerateTechRubric(item.AssessmentGroupName);

                            if (result != null)
                            {
                                var model = JsonConvert.DeserializeObject<RubricSchema>(result.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value);
                                var techSkills = new List<TransSkill>();
                                if (model != null && model.Rubric != null)
                                {
                                    foreach (var skill in model.Rubric)
                                    {
                                        var techSkill = new TransSkill();

                                        string levelDesc = skill.ProficiencyLevel; // The string to match

                                        // Parse the string into an enum value
                                        if (Enum.TryParse(typeof(LevelTechEnum), levelDesc, true, out var parsedLevel))
                                        {
                                            // Cast the result to its integer value
                                            techSkill.LevelId = (int)parsedLevel;
                                        }
                                        techSkill.SkillName = skill.Skill;
                                        techSkill.Weightage = skill.Weight;
                                        if (skill.CoreConcepts != null)
                                        {
                                            techSkill.Concepts = string.Join(",", skill.CoreConcepts);
                                        }
                                        techSkill.Natureofquestion = skill.KeyExpectations;
                                        techSkill.Timelimit = Convert.ToInt16(skill.EvaluationTime);
                                        techSkill.isEliminationskill = (skill.Mandatory != null && skill.Mandatory.ToLower() == "yes") ? true : false;

                                        techSkills.Add(techSkill);

                                    }
                                    await _cscRepository.RenewTemplateRubrics(techSkills, item.ClonableSkillChannelId);
                                    //save the rubrics in db
                                }
                                
                            }
                        }
                        catch (Exception ex)
                        {
                            log.LogInformation($"AIQualityCheckBatchRun : C# Timer trigger {ex.Message}");
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        }

                    }


                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            log.LogInformation($"AIQualityCheckBatchRun : C# Timer trigger function executed at: {DateTime.Now}");

        }


    }
}
