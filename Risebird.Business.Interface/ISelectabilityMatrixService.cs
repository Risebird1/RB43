using Risebird.Entities.Data;
using System.Collections.Generic;

namespace Risebird.Business.Interface
{
    public interface ISelectabilityMatrixService
    {
        List<SelectabilityMatrix> GetSelectabilityMatrix();
        bool AddSelectabilityMatrix(SelectabilityMatrix maxtrix);
        SelectabilityMatrix GetSelectabilityMatrixById(int id);
        bool UpdateSelectabilityMatrix(SelectabilityMatrix selectableToUpdate);
        bool DeleteSelectabilityMatrix(int id);
    }
}
