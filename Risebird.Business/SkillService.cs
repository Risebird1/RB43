using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Risebird.Business.Interface;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;
using Risebird.Entities.ViewModel;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;

namespace Risebird.Business
{
    public class SkillService : ISkillService
    {
        private ISkillRepository _skillRepository;
        private IApplicationErrorLogger _applicationErrorLogger;

        public SkillService(ISkillRepository skillRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _skillRepository = skillRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public List<Skill> GetSkills()
        {
            try
            {
                return _skillRepository.GetSkills().ToList();
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public List<SkillLevel> GetSkillLevels()
        {
            try
            {
                return _skillRepository.GetSkillLevels().ToList();
            }
            catch (Exception e)
            {
                return null;
            }
        }
        
        public bool AddSkillLevel(SkillLevel skillLevel)
        {
            try
            {
                _skillRepository.AddSkillLevel(skillLevel);
                return true;
            }

            catch(Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public SkillLevel GetSkillLevel(int id)
        {
            try
            {
                if (_skillRepository.GetSkillLevel(id) == null)
                    return null;
                else
                    return _skillRepository.GetSkillLevel(id).Result;
                        
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
                _skillRepository.UpdateSkillLevel(skillLevelToUpdate);
                return true;
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
                _skillRepository.DeleteSkillLevel(id);
                return true;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
    }
}
