using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IGeneralSkillMatrixService
    {
        List<GeneralSkillMatrix> GetGeneralSkillMatrix();
        bool AddGeneralSkillMatrix(GeneralSkillMatrix maxtrix);
        GeneralSkillMatrix GetGeneralSkillMatrixByiId(int id);
        bool UpdateGeneralSkillMatrix(GeneralSkillMatrix generalToUpdate);
        bool DeleteGeneralSkillMatrix(int id);
    }
}
