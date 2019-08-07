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
        IEnumerable<Interviewer> GetInterviewerSkills();
        IEnumerable<TransSkill> GetSkillsbyIRFid(int id);
        IEnumerable<TransClientUser> GetTransClientUser(int id);
        IEnumerable<TransInterviewer> GetTransInterviewer(int id);
        IEnumerable<TransJoinability> GetTransJoinability(int id);
        IEnumerable<TransQualityController> GetTransQualityController(int id);
        IEnumerable<TransSkillGeneral> GetSkillsGeneralbyIRFid(int id);
        bool AddSkillComments(TechSkillComments obj);
        Task<IEnumerable<string>> GetListOfInterviwerBasedOnIRFId(string irfId);
        Task<bool> DeleteIRF(int IRFId);
        IEnumerable<MeetingVM> GetIRFInterviewMeetings(int id);
        string GetQCSkills(int userId);
        Task<bool> UpdateIRFDetails(IRFVm irfDetails);
        IEnumerable<SkillComment> GetTechSkillComments(int id);
        IEnumerable<TransSelectability> GetTransSelectability(int id);
        IEnumerable<LevelTech> GetLevelTech();
        bool CheckUniqueness(string value);
        IEnumerable<GeneralSkillMatrix> GetGeneralSkillMatrix();
        IEnumerable<SelectabilityMatrix> GetSelectabilityMatrix();
        IEnumerable<JoinabilityMatrix> GetJoinabilityMatrix();

    }
}
