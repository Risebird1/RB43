using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IAadhaarService
    {
        Task<bool> SaveAadhaarDetails(AadhaarData model);
        Task<AadhaarData> FetchAadhaarDetails(string MCQId, string candidateEmail);
        Task<AadhaarDetails> FetchCandidateEmailidByUniqueId(string uniqueid);
        Task<string> FetchUniqueIdByEmailid(string emailid);
        Task<AadhaarData> FetchInterviewAadhaarDetails(string candidateEmail);
        Task<IEnumerable<AadhaarData>> GetAadharDetails();
    }
}
