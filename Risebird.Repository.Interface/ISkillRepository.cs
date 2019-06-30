using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Repository.Interface
{
    public interface ISkillRepository
    {
        IEnumerable<SkillLevel> GetSkillLevels();
        IEnumerable<Skill> GetSkills();
    }
}
