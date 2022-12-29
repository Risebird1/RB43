using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
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

        Task<IEnumerable<Skill>> GetSkills();
        Task<bool> AddSkill(Skill skill);
        Task<Skill> GetSkill(int id);
        Task<bool> UpdateSkill(Skill entity);
        Task<bool> DeleteSkill(int id);
        Task<IEnumerable<Skill>> GetAssessmentGroupCategory();

        Task<IEnumerable<AssignmentVM>> GetAssessmentGroupsByCategory(int categoryId);
        Task<int?> GetAssessmentGroupCategory(int assessmentGroupId);
        Task<IEnumerable<string>> GetPrimarySkillsBasedOnCategory(int categoryId);
        Task<IEnumerable<Skill>> GetMCQSetsDetails();
    }
}
