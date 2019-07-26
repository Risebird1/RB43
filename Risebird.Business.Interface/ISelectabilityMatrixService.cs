using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;

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
