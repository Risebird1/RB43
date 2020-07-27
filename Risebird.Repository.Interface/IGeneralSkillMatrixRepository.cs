using Risebird.Entities.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IGeneralSkillMatrixRepository
    {
        IEnumerable<GeneralSkillMatrix> GetGeneralSkillMatrix();
        Task<GeneralSkillMatrix> GetGeneralSkillMatrixByiId(int id);
        Task<bool> AddGeneralSkillMatrix(GeneralSkillMatrix maxtrix);
        Task<bool> UpdateGeneralSkillMatrix(GeneralSkillMatrix entity);
        Task<bool> DeleteGeneralSkillMatrix(int id);
    }
}
