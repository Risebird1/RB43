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
    public class SkillService : ISkillService
    {
        private readonly ISkillRepository _skillRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public SkillService(ISkillRepository skillRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _skillRepository = skillRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        #region Skill Functions

        public List<Skill> GetSkills()
        {
            try
            {
                return _skillRepository.GetSkills().ToList();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool AddSkill(Skill skill)
        {
            try
            {
                return _skillRepository.AddSkill(skill).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public Skill GetSkill(int id)
        {
            try
            {
                var skill = _skillRepository.GetSkill(id);
                if (skill == null)
                    return null;
                else
                    return skill.Result;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool UpdateSkill(Skill skillToUpdate)
        {
            try
            {
                return _skillRepository.UpdateSkill(skillToUpdate).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public bool DeleteSkill(int id)
        {
            try
            {
                return _skillRepository.DeleteSkill(id).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        #endregion

        #region Skill Level Fucntions

        public List<SkillLevel> GetSkillLevels()
        {
            try
            {
                return _skillRepository.GetSkillLevels().ToList();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool AddSkillLevel(SkillLevel skillLevel)
        {
            try
            {
                return _skillRepository.AddSkillLevel(skillLevel).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public SkillLevel GetSkillLevel(int id)
        {
            try
            {
                var skillLevel = _skillRepository.GetSkillLevel(id);
                if (skillLevel == null)
                    return null;
                else
                    return skillLevel.Result;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool UpdateSkillLevel(SkillLevel skillLevelToUpdate)
        {
            try
            {
                return _skillRepository.UpdateSkillLevel(skillLevelToUpdate).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public bool DeleteSkillLevel(int id)
        {
            try
            {
                return _skillRepository.DeleteSkillLevel(id).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        #endregion
        public List<Skill> GetAssessmentGroupCategory()
        {
            try
            {
                return _skillRepository.GetAssessmentGroupCategory().ToList();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IEnumerable<AssignmentVM>> GetAssessmentGroupsByCategory(int categoryId)
        {
            try
            {
                return await _skillRepository.GetAssessmentGroupsByCategory(categoryId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

    }
}
