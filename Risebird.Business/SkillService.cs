using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Risebird.Business.Interface;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;

namespace Risebird.Business
{
    public class SkillService:ISkillService
    {
        private ISkillRepository _skillRepository;
        public SkillService(ISkillRepository skillRepository)
        {
            _skillRepository = skillRepository;
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
    }
}
