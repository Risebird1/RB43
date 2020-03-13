using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Business.Interface
{
    public interface ISupportService
    {
        List<Support> GetAllSupportHR();
        bool AddSupportHR(Support hrDetails);
        bool UpdateSupportHR(Support hrDetails);
        Support GetSupportHR(int id);
        bool DeleteSupportHR(int id);
    }
}
