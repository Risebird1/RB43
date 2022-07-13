using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ISupportRepository
    {
        IEnumerable<Support> GetAllSupportHR(string emailId = null);
        Task<bool> AddSupportHR(Support hrDetails);
        bool UpdateSupportHR(Support hrDetails);
        Support GetSupportHR(int id);
        bool DeleteSupportHR(int id);
        IEnumerable<string> GetAllSupportHREmails();
    }
}
