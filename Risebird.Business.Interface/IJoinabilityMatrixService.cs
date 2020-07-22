using Risebird.Entities.Data;
using System.Collections.Generic;

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
