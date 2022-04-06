using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IRecommendedIRFRepository
    {
        Task<IEnumerable<SkillCahnnelDetailsVM>> GetFetchRecommendedIRF();
        IRF FetchClonableIRF(int irfId);
        IEnumerable<TransSkill> GetClonableIRFSkillsbyIRFid(int id);
        IEnumerable<TransInterviewer> GetClonableIRFTransInterviewer(int id);
        IEnumerable<TransHR> GetClonableIRFTransHR(int id);
        IEnumerable<SkillComment> GetClonableIRFQuestiondiscussed(int id);
        bool AddRecommendationIRFDetails(IRFVm irfDetails, string loggedInUser, out int irfID);
        Task<bool> UpdateRecommendationIRFDetails(IRFVm irfDetails);
        Task<IEnumerable<IRF>> GetAllRecommendedIRFs(string emailID);
    }
}
