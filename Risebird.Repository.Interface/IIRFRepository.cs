using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IIRFRepository
    {
        IEnumerable<Level> GetLevels();
        IEnumerable<Skill> GetSkills();
        IEnumerable<Users> GetCientUsers();
        IEnumerable<Client> GetClients();
        IEnumerable<Users> GetUsers();
        IEnumerable<InterviewerVM> GetInterviewers();
        IEnumerable<ClientRole> GetClientRoles();
        IEnumerable<Users> GetQualityController();
        Task<bool> AddIRFDetails(IRFVm irfDetails);
        IEnumerable<IRF> GetAllIRF();
    }
}
