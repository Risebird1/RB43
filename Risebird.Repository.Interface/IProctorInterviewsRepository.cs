using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IProctorInterviewsRepository
    {
        Task<IEnumerable<VMProperties>> GetAllVMs();
        Task<IEnumerable<TriggeredVM>> GetAllTriggeredVMs();
        Task<bool> DeleteTriggeredVMs(List<int> idList);
        Task<bool> UpdateVMStatus(int id, int status);
    }
}
