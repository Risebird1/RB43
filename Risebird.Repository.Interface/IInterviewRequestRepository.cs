using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IInterviewRequestRepository
    {
        Task<bool> CreateInerviewRequest(InterviewRequestVM meeting);
        Task<IEnumerable<InterviewRequestVM>> GetAllInterviewRequests(string emailID, string profileName);
        Task<InterviewRequestVM> FetchInterviewRequestById(int id);
        Task<bool> AssignInerviewRequest(int userid, int id);
        Task<bool> SetIRFCreated(int id, string IRFId);
        Task<string> FetchInterviewerNameByEmailid(string emailid);
    }
}
