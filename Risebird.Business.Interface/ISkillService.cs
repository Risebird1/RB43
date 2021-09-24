using Risebird.Entities.Data;
using System.Collections.Generic;

namespace Risebird.Business.Interface
{
    public interface ISkillService
    {
        List<Skill> GetSkills();
        bool AddSkill(Skill skill);
        Skill GetSkill(int id);
        bool UpdateSkill(Skill skillToUpdate);
        bool DeleteSkill(int id);

        List<SkillLevel> GetSkillLevels();
        bool AddSkillLevel(SkillLevel skillLevel);
        SkillLevel GetSkillLevel(int id);
        bool UpdateSkillLevel(SkillLevel skillLevelToUpdate);
        bool DeleteSkillLevel(int id);
        List<Skill> GetAssessmentGroupCategory();

    }
}
