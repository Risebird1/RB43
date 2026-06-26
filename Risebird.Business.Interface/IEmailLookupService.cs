using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IEmailLookupService
    {
        Task<EmailLookupResultVM> GetEmailLookupData(string emailId);
    }
}
