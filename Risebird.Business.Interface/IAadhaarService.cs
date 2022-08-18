using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IAadhaarService
    {
        Task<bool> SaveAadhaarDetails(AadhaarData model);
        Task<AadhaarData> FetchAadhaarDetails(string MCQId, string candidateEmail);
        Task<string> FetchCandidateEmailidByUniqueId(string uniqueid);
    }
}
