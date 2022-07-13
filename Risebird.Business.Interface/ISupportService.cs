using Risebird.Entities.ViewModel;
using System.Collections.Generic;

namespace Risebird.Business.Interface
{
    public interface ISupportService
    {
        List<Support> GetAllSupportHR(string emailId = null);
        bool AddSupportHR(Support hrDetails);
        bool UpdateSupportHR(Support hrDetails);
        Support GetSupportHR(int id);
        bool DeleteSupportHR(int id);
    }
}
