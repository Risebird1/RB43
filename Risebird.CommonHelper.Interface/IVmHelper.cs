using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IVmHelper
    {
        Task<string> Fetchavailablelocation();
        Task<string> CreateVmMachine(VMProperties model);
        Task<string> DeleteVM(string resoucegroup, string vmname);
        Task<string> DeleteResourcegroup(string resoucegroup);
        Task<string> DeallocateVM(string resoucegroup, string vmname);
    }
}
