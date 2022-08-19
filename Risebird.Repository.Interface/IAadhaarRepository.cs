using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IAadhaarRepository
    {
        Task<bool> SaveAadhaarDetails(AadhaarData model);
        Task<AadhaarData> FetchAadhaarDetails(string MCQId, string candidateEmail);
        Task<string> FetchCandidateEmailidByUniqueId(string uniqueid);
    }
}
