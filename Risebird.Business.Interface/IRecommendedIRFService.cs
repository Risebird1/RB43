using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IRecommendedIRFService
    {
        IEnumerable<SkillCahnnelDetailsVM> GetFetchRecommendedIRF();
        Task<IRFVm> GetRecommendationIRFByIRFid(int id, bool isClone = false);
        Task<bool> AddRecommendationIRFDetails(IRFVm irfDetails, string loggedInUser, string domain);
        Task<bool> UpdateRecommendationIRFDetails(IRFVm irfDetails, string loggedInUser);
        IEnumerable<IRF> GetAllRecommendedIRFs(string emailId);
    }
}
