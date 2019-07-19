using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
   public interface IJoinabilityMatrixRepository
    {
        IEnumerable<JoinabilityMatrix> GetJoinabilityMatrix();
        Task<bool> AddJoinabilityMatrix(JoinabilityMatrix maxtrix);
        Task<bool> UpdateJoinabilityMatrix(JoinabilityMatrix entity);
        Task<bool> DeleteJoinabilityMatrix(int id);
        Task<JoinabilityMatrix> GetJoinabilityMatrixById(int id);
    }
}
