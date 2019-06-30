using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Business.Interface
{
    public interface ISkillService
    {
        List<Skill> GetSkills();
        List<SkillLevel> GetSkillLevels();
    }
}
