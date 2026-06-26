using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IEmailLookupRepository
    {
        Task<EmailLookupResultVM> GetEmailLookupData(string emailId);
    }
}
