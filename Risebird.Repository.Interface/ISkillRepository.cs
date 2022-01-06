using Risebird.Entities.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ISkillRepository
    {
        IEnumerable<SkillLevel> GetSkillLevels();
        Task<bool> AddSkillLevel(SkillLevel skillLevel);
        Task<SkillLevel> GetSkillLevel(int id);
        Task<bool> UpdateSkillLevel(SkillLevel entity);
        Task<bool> DeleteSkillLevel(int id);

        IEnumerable<Skill> GetSkills();
        Task<bool> AddSkill(Skill skill);
        Task<Skill> GetSkill(int id);
        Task<bool> UpdateSkill(Skill entity);
        Task<bool> DeleteSkill(int id);
        IEnumerable<Skill> GetAssessmentGroupCategory();
    }
}
