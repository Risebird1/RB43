using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IProctorInterviewsService
    {
        Task<IEnumerable<VMProperties>> GetAllVMs();
        Task<IEnumerable<VMProperties>> GetAllUserVMs(string emailId, string profile);
        Task<IEnumerable<TriggeredVM>> GetAllTriggeredVMs();
        Task<bool> DeleteTriggeredVMs(List<int> ids);
        Task<List<NonExistingVMDTO>> GetNonExistingVMs();
        Task<string> AddNonExistingVMs(List<NonExistingVMDTO> vmList);
        Task<string> ResetPassword(VMResetPassword model);
        Task<bool> UpdateVMNamePassword(int id, string customName, string password, string userName);
        IEnumerable<TransVMUser> GetUserList(int vmId, int profileId);
        Task<bool> UpdateVMUsers(int[] userList, int vmId, int profileId);
        Task<IEnumerable<ProctorVMQuestions>> GetVMQuestions(int vmId);
        Task<bool> UpdateVMQuestions(ProctorQuestionsViewModel model);
        Task<bool> UpdateActionPerformed(int vmId, string emailId, string action);
        Task<bool> RemoveVM(int vmId);
        Task<string> AddNewVmDetails(VMProperties model);
    }
}
