using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IProctorInterviewsService
    {
        Task<IEnumerable<VMProperties>> GetAllVMs();
        Task<IEnumerable<TriggeredVM>> GetAllTriggeredVMs();
        Task<bool> DeleteTriggeredVMs(List<int> ids);
        Task<List<NonExistingVMDTO>> GetNonExistingVMs();
        Task<string> AddNonExistingVMs(List<NonExistingVMDTO> vmList);
    }
}
