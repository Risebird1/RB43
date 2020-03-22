using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class IRFViewModel
    {
        public IEnumerable<IRF> IRFList { get; set; }
        public string Profile { get; set; }
        public string Message { get; set; }
        public bool IsClientUser  { get; set; }
        public List<string> RoleList { get; set; }
    }
}
