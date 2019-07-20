using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Business.Interface
{
  public interface IJoinabilityMatrixService
    {
        List<JoinabilityMatrix> GetJoinabilityMatrix();
        bool AddJoinabilityMatrix(JoinabilityMatrix maxtrix);
        JoinabilityMatrix GetJoinabilityMatrixById(int id);
        bool UpdateJoinabilityMatrix(JoinabilityMatrix joinableToUpdate);
        bool DeleteJoinabilityMatrix(int id);
    }
}
