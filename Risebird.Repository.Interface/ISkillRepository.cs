using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ISkillRepository
    {
        IEnumerable<SkillLevel> GetSkillLevels();
        IEnumerable<Skill> GetSkills();

        Task<bool> AddSkillLevel(SkillLevel skillLevel);
        Task<SkillLevel> GetSkillLevel(int id);
        Task<bool> UpdateSkillLevel(SkillLevel entity);
        Task<bool> DeleteSkillLevel(int id);
    }
}
