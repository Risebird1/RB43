using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IIRFService
    {
        Task<bool> AddIRFDetails(IRFVm irfDetails);
        IEnumerable<Users> GetRiseBirdHR();
        string GetInterviewerSkills(int id);
        IEnumerable<Skill> GetTechSkills(int id);
        bool AddSkillComments(TechSkillComments skillComments);
        List<string> GetListOfInterviwerBasedOnIRFId(string irfId);
    }
}
