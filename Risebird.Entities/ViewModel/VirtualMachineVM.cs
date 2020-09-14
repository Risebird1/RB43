using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class VirtualMachineVM
    {
        public List<VMProperties> VMList { get; set; }
        public string Message { get; set; }
    }
}
