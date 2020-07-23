using Risebird.Entities.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ISelectabilityMatrixRepository
    {
        IEnumerable<SelectabilityMatrix> GetSelectabilityMatrix();
        Task<bool> AddSelectabilityMatrix(SelectabilityMatrix maxtrix);
        Task<bool> UpdateSelectabilityMatrix(SelectabilityMatrix entity);
        Task<bool> DeleteSelectabilityMatrix(int id);
        Task<SelectabilityMatrix> GetSelectabilityMatrixById(int id);
    }
}
