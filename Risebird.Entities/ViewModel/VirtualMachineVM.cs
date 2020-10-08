using Risebird.Entities.Data;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class VirtualMachineVM
    {
        public List<VMProperties> VMList { get; set; }
        public string Message { get; set; }
        public string Profile { get; set; }
    }
}
