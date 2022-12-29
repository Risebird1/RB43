using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface ISkillService
    {
        Task<List<Skill>> GetSkills();
        bool AddSkill(Skill skill);
        Skill GetSkill(int id);
        bool UpdateSkill(Skill skillToUpdate);
        bool DeleteSkill(int id);

        List<SkillLevel> GetSkillLevels();
        bool AddSkillLevel(SkillLevel skillLevel);
        SkillLevel GetSkillLevel(int id);
        bool UpdateSkillLevel(SkillLevel skillLevelToUpdate);
        bool DeleteSkillLevel(int id);
        Task<List<Skill>> GetAssessmentGroupCategory();

        Task<IEnumerable<AssignmentVM>> GetAssessmentGroupsByCategory(int categoryId);
        Task<int?> GetAssessmentGroupCategory(int assessmentGroupId);
        Task<IEnumerable<string>> GetPrimarySkillsBasedOnCategory(int categoryId);
        Task<List<Skill>> GetMCQSetsDetails();
    }
}
