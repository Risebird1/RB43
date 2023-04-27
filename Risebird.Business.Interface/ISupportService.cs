using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface ISupportService
    {
        Task<List<Support>> GetAllSupportHR(string emailId = null);
        Task<bool> AddSupportHR(Support hrDetails);
        Task<bool> UpdateSupportHR(Support hrDetails);
        Task<Support> GetSupportHR(int id);
        Task<bool> DeleteSupportHR(int id);
        Task<bool> SaveRaisedIssueDetails(RaiseBugsVM model);
        Task<IEnumerable<RaiseBugsVM>> GetRaisedIssueDetails();
    }
}
