using Risebird.Entities.Data;
using System.Collections.Generic;

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
