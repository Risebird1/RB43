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
        Task<IEnumerable<VMProperties>> GetAllUserVMs(string emailId, string profile);
        Task<IEnumerable<TriggeredVM>> GetAllTriggeredVMs();
        Task<bool> DeleteTriggeredVMs(List<int> idList);
        Task<bool> UpdateVMStatus(int id, int status);
        Task<bool> AddVMTrigger(int vmId, string action, string expectedStatus);
        Task<string> AddNonExistingVMs(List<VMProperties> vmList);
        int GetVM(string name, string rgName);
        Task<bool> AddBulkVmTrigger(List<TriggeredVM> vmList);
        VMProperties GetVMbyId(int id);
        Task UpdateVMIP(IList<VMProperties> vmList);
    }
}
