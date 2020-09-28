using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class VMUsersMapping
    {
        public int VmId { get; set; }
        public string  VmName { get; set; }
        public IList<TransVMUser> UsersList { get; set; }
    }
}
