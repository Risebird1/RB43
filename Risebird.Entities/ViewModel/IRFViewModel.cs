using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class IRFViewModel
    {
        public IEnumerable<IRF> IRFList { get; set; }
        public IEnumerable<IRF> IRFFilterList { get; set; }
        public string Profile { get; set; }
        public string Message { get; set; }
        public bool IsClientUser { get; set; }
        public List<string> RoleList { get; set; }
    }
}
