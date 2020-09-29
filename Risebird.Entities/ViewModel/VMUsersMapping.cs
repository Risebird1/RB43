using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class VMUsersMapping
    {
        public int VmId { get; set; }
        public string VmName { get; set; }
        public IList<TransVMUser> UsersList { get; set; }
    }
}
