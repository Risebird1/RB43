using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
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

        Task<bool> UpdateVMNamePassword(int id, string customName, string password, string userName);
        Task<bool> UpdateVMPassword(int id, string password);
        IEnumerable<TransVMUser> GetUserList(int vmId, int profileId);
        Task<bool> UpdateVMUsers(int[] userList, int vmId, int profileId);
        Task<IEnumerable<ProctorVMQuestions>> GetVMQuestions(int vmId);
        Task<bool> UpdateVMQuestions(ProctorQuestionsViewModel model);

        Task<bool> UpdateActionPerformed(int vmId, string emailId, string action);
        Task<bool> RemoveCandidateVmAccess();
        Task<bool> RemoveVM(int vmId);
        Task<IEnumerable<VMActionPerformed>> GetVMStarterEmailId();
        Task<IEnumerable<VMProperties>> GetDeleteorShutdownVMList();
    }
}
