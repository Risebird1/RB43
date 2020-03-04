using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ISupportRepository
    {
        IEnumerable<Support> GetAllSupportHR();
        Task<bool> AddSupportHR(Support hrDetails);
        bool UpdateSupportHR(Support hrDetails);
        Support GetSupportHR(int id);
        bool DeleteSupportHR(int id);
    }
}
