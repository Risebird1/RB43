using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IAadhaarRepository
    {
        Task<bool> SaveAadhaarDetails(AadhaarData model);
    }
}
